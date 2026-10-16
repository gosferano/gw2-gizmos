using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Gw2Gizmos.Avalonia.Hosting;
using Gw2Gizmos.Avalonia.Navigation;
using Gw2Gizmos.Desktop.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Gw2Gizmos.Avalonia;

/// <summary>
/// Application lifecycle. Runs as a tray app that hosts the lightweight delivery-notification poller in-process and
/// launches the heavy ingestion engine as a separate worker process so it can't stall the UI. Both share the same
/// database and API key.
/// </summary>
public partial class App : Application
{
#if DEBUG
    private const string SingleInstanceMutexName = @"Local\Gw2Gizmos.Dev.SingleInstance";
#else
    private const string SingleInstanceMutexName = @"Local\Gw2Gizmos.SingleInstance";
#endif

    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private IHost? _host;
    private MainWindow? _window;
    private Process? _workerProcess;
    private Mutex? _singleInstanceMutex;
    private bool _isExiting;

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

        // One instance per user: a second launch would spawn a duplicate worker and double every notification.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isPrimary);
        if (!isPrimary)
        {
            desktop.Shutdown(0);
            return;
        }

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
            // The host's console lifetime turns SIGTERM / Ctrl+C (e.g. a session logout) into a host stop; make that
            // shut down the whole app — window, tray and worker — not just the background services.
            _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(
                () => Dispatcher.UIThread.Post(Shutdown));
            // The worker owns the database and is its sole migrator/writer; the desktop opens it read-only.
            await _host.StartAsync();

            // The worker shares the same database and fetches its config over the pipe whose name it's given.
            _workerProcess = _host.Services.GetRequiredService<WorkerProcessLauncher>().Start();

            // A dev build (run from bin) isn't Velopack-installed; suffix its title so it's distinguishable.
            bool isInstalled = _host.Services.GetRequiredService<AppUpdateService>().IsInstalled;
            string appTitle = isInstalled ? "Gw2Gizmos" : "Gw2Gizmos (development)";
            if (TrayIcon.GetIcons(this) is { Count: > 0 } trayIcons)
            {
                trayIcons[0].ToolTipText = appTitle;
            }

            _window = _host.Services.GetRequiredService<MainWindow>();
            _window.Title = appTitle;
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
        }
        catch (Exception ex)
        {
            HandleFatal(ex, "startup");
        }
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
        // Released as the handle closes, freeing the single-instance slot for the next launch.
        _singleInstanceMutex?.Dispose();

        try
        {
            if (_workerProcess is { HasExited: false })
            {
                _workerProcess.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop the worker process.");
        }

        if (_host is not null)
        {
            // Exit is synchronous; give the hosted services a bounded chance to stop.
            _host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            _host.Dispose();
        }

        Log.CloseAndFlush();
    }
}
