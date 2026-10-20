using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FluentAvalonia.Styling;
using Gw2Gizmos.Desktop.Hosting;
using Gw2Gizmos.Desktop.Navigation;
using Gw2Gizmos.Desktop.Ui;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Application lifecycle. Runs as a tray app that hosts the lightweight delivery-notification poller in-process and
/// launches the heavy ingestion engine as a separate worker process so it can't stall the UI. Both share the same
/// database and API key.
/// </summary>
public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private IHost? _host;
    private MainWindow? _window;
    private WorkerSupervisor? _worker;
    private bool _isExiting;

    /// <summary>This process's claim on being the app's one instance, taken (and released) by <see cref="Program"/>.</summary>
    internal static SingleInstance? Instance { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        _desktop = desktop;
        // Closing the window only hides it to the tray; the app exits from the tray menu.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        desktop.Exit += OnExit;

        RegisterGlobalExceptionHandlers();
        _ = StartApplicationAsync(desktop.Args ?? Array.Empty<string>());
    }

    private async Task StartApplicationAsync(string[] args)
    {
        try
        {
            string dataDirectory = AppDataDirectory.Ensure();

            // Random, ephemeral name for the local pipe that serves the live worker config (keys + features +
            // intervals) to the worker, so it never reads the desktop's secret file.
            string workerConfigPipeName = $"Gw2Gizmos.Config.{Guid.NewGuid():N}";

            _host = AppHostFactory.Build(dataDirectory, args, workerConfigPipeName);
            AppServices.Navigation = _host.Services.GetRequiredService<AvaloniaNavigationService>();
            FollowSystemAccentColor(_host.Services.GetRequiredService<ISystemAccentColor>());
            AppServices.Rarities = _host.Services.GetRequiredService<ItemRarities>();
            AppServices.Rarities.StartLoading();
            AppServices.ItemLinks = _host.Services.GetRequiredService<ItemLinks>();
            AppServices.ItemLinks.StartLoading();
            AppServices.Icons = new IconProvider<Bitmap>(
                _host.Services.GetRequiredService<IServiceScopeFactory>(), dataDirectory, AvaloniaIconDecoder.Decode);
            AppServices.Tiles = new MapTileCache<Bitmap>(dataDirectory, AvaloniaTileDecoder.Decode);
            // The host's console lifetime turns SIGTERM / Ctrl+C (e.g. a session logout) into a host stop; make that
            // shut down the whole app — window, tray and worker — not just the background services.
            _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(
                () => Dispatcher.UIThread.Post(Shutdown));
            // The worker owns the database and is its sole migrator/writer; the desktop opens it read-only.
            await _host.StartAsync();

            // The worker shares the same database and fetches its config over the pipe whose name it's given. The
            // supervisor restarts it if it dies.
            _worker = _host.Services.GetRequiredService<WorkerSupervisor>();
            _worker.Start();

            // A dev build (run from bin) isn't Velopack-installed; suffix its title so it's distinguishable.
            bool isInstalled = _host.Services.GetRequiredService<AppUpdateService>().IsInstalled;
            string appTitle = isInstalled ? "Gw2Gizmos" : "Gw2Gizmos (development)";
            if (TrayIcon.GetIcons(this) is { Count: > 0 } trayIcons)
            {
                trayIcons[0].ToolTipText = appTitle;
            }

            _window = _host.Services.GetRequiredService<MainWindow>();
            _window.Title = appTitle;
            // The lifetime's main window is where services find a window to work through — the clipboard for
            // "copy" buttons (in notifications and on the Events page) among them. It doesn't decide shutdown here
            // (that's explicit), so hiding it to the tray is unaffected.
            _desktop!.MainWindow = _window;
            _window.Closing += (_, e) =>
            {
                if (!_isExiting)
                {
                    // Close hides to the tray instead of exiting.
                    e.Cancel = true;
                    _window.Hide();
                }
            };

            // Launched at login (--minimized): stay in the tray.
            if (!args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
            {
                _window.Show();
            }

            // From here on, launching the app again (e.g. from the launcher while it's in the tray) shows this window.
            Instance?.ListenForShowRequests(() => Dispatcher.UIThread.Post(ShowWindow));
        }
        catch (Exception ex)
        {
            HandleFatal(ex, "startup");
        }
    }

    /// <summary>
    /// Uses the OS accent the platform reports where FluentAvalonia can't read it itself (e.g. GNOME's accent on
    /// Hyprland), and keeps it current as it changes; when the platform reports none, FluentAvalonia's own stands.
    /// </summary>
    private void FollowSystemAccentColor(ISystemAccentColor accent)
    {
        if (Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault() is not { } theme)
        {
            return;
        }

        void Apply() => theme.CustomAccentColor = accent.Current is { } argb ? Color.FromUInt32(argb) : null;

        Apply();
        accent.Changed += () => Dispatcher.UIThread.Post(Apply);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        // Nothing should crash without a trace: last-resort logging for background-thread failures.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception; the process is terminating.");
            Log.CloseAndFlush();
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception.");
            args.SetObserved();
        };
        // An error in a UI update (a binding, a list's change notification) would otherwise end the whole app — and
        // this one lives in the tray all night. Log it and carry on.
        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception on the UI thread; carrying on.");
            args.Handled = true;
        };
    }

    /// <summary>Logs a fatal error and exits, so a failure is never silent.</summary>
    private void HandleFatal(Exception ex, string origin)
    {
        Log.Fatal(ex, "Fatal error during {Origin}; Gw2Gizmos will close.", origin);
        Console.Error.WriteLine($"Gw2Gizmos hit a fatal error during {origin}: {ex}");
        Shutdown();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => ShowWindow();

    private void OnTrayShowClicked(object? sender, EventArgs e) => ShowWindow();

    private void OnTrayExitClicked(object? sender, EventArgs e) => Shutdown();

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    private void Shutdown()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _desktop?.Shutdown();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _worker?.Stop();

        if (_host is not null)
        {
            // Exit is synchronous; give the hosted services a bounded chance to stop.
            _host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            _host.Dispose();
        }

        Log.CloseAndFlush();
    }
}
