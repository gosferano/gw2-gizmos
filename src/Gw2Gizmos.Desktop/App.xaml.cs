using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Velopack;
using Velopack.Sources;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Tray.Controls;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Application entry point. Runs as a tray app that hosts the lightweight delivery-notification
/// poller (with Windows toasts) in-process, and launches the heavy ingestion engine as a separate
/// worker process so it can't stall the UI. Both share the same database and API key.
/// </summary>
public partial class App : Application
{
    /// <summary>Item-icon cache, exposed statically so the lightweight <c>ItemImage</c> control can reach it.</summary>
    public static IconProvider? Icons { get; private set; }

    private NotifyIcon? _trayIcon;
    private MainWindow? _window;
    private IHost? _host;
    private Process? _workerProcess;
    private readonly ChildProcessJob _workerJob = new();
    private Mutex? _singleInstanceMutex;
    private bool _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Velopack's hooks run in Program.Main (before WPF starts); here we just begin a normal launch.
        base.OnStartup(e);

        // One instance per user: a second launch (Start-menu + tray, or a Velopack post-update relaunch
        // racing the old process) would spawn a duplicate worker and double every toast. Bail if held.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, @"Local\Gw2Gizmos.SingleInstance", out bool isPrimary);
        if (!isPrimary)
        {
            Shutdown(0);
            return;
        }

        RegisterGlobalExceptionHandlers();

        // OnStartup is async void, so an unhandled throw here would tear the process down silently —
        // the class of failure a missing tray-icon resource once caused. Surface it instead.
        try
        {
            await StartApplicationAsync(e.Args);
        }
        catch (Exception ex)
        {
            HandleFatal(ex, "startup");
        }
    }

    private async Task StartApplicationAsync(string[] args)
    {
        ToastService.RegisterAppId();

        string dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gw2Gizmos"
        );
        Directory.CreateDirectory(dataDir);
        string dbPath = Path.Combine(dataDir, "gw2gizmos.sqlite");

        (IHost host, string connectionString, string dbProvider) = BuildHost(dataDir, dbPath, args);
        _host = host;
        _host.Services.MigrateGw2GizmosDb();
        await _host.StartAsync();

        Icons = new IconProvider(_host.Services.GetRequiredService<IServiceScopeFactory>(), dataDir);

        // The worker shares the same database, so it must use the same provider + connection string.
        StartWorkerProcess(dataDir, connectionString, dbProvider);

        // A dev build (run from bin) isn't Velopack-installed; suffix its window title + tray tooltip so
        // it's distinguishable from the installed app when both are running at once.
        UpdateManager updateManager = CreateUpdateManager();
        string appTitle = TryIsInstalled(updateManager) ? "Gw2Gizmos" : "Gw2Gizmos (development)";

        _window = _host.Services.GetRequiredService<MainWindow>();
        _window.Title = appTitle;
        _window.Closing += (_, args) =>
        {
            if (!_isExiting)
            {
                // Close button hides to the tray instead of exiting.
                args.Cancel = true;
                _window.Hide();
            }
        };
        _window.Show();

        SetupTrayIcon(appTitle);

        // Check for updates in the background; no-op when run from bin (not Velopack-installed).
        _ = CheckForUpdatesAsync(updateManager);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        // Nothing should crash without a trace. A UI-thread fault is recoverable to a dialog + log;
        // the AppDomain/Task handlers are last-resort logging for background-thread failures.
        DispatcherUnhandledException += (_, args) =>
        {
            HandleFatal(args.Exception, "the UI thread");
            args.Handled = true;
        };
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

    /// <summary>Logs a fatal error, tells the user, and exits — so a failure is never silent.</summary>
    private void HandleFatal(Exception? ex, string origin)
    {
        Log.Fatal(ex, "Fatal error during {Origin}; Gw2Gizmos will close.", origin);
        Log.CloseAndFlush();

        try
        {
            MessageBox.Show(
                $"Gw2Gizmos hit a fatal error and needs to close.\n\n{ex?.Message}",
                "Gw2Gizmos",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        catch
        {
            // A dialog may not be possible this early; the log above is the fallback.
        }

        Shutdown(1);
    }

    private static UpdateManager CreateUpdateManager() =>
        new(new GithubSource("https://github.com/gosferano/gw2-gizmos", null, prerelease: false));

    /// <summary>True for a Velopack-installed build, false for a dev/bin run; never throws.</summary>
    private static bool TryIsInstalled(UpdateManager updateManager)
    {
        try
        {
            return updateManager.IsInstalled;
        }
        catch
        {
            return false;
        }
    }

    private static async Task CheckForUpdatesAsync(UpdateManager updateManager)
    {
        try
        {
            if (!updateManager.IsInstalled)
            {
                return;
            }

            UpdateInfo? update = await updateManager.CheckForUpdatesAsync();
            if (update is not null)
            {
                // Download now; Velopack applies it on the next restart so the session isn't interrupted.
                await updateManager.DownloadUpdatesAsync(update);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed.");
        }
    }

    private static (IHost Host, string ConnectionString, string Provider) BuildHost(
        string dataDir,
        string dbPath,
        string[] args
    )
    {
        // Pass args so --Database:Provider / --ConnectionStrings:Gw2GizmosDb (and matching env vars) flow
        // into configuration.
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        string environment = builder.Environment.EnvironmentName.ToLowerInvariant();
        const string logOutputTemplate =
            "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{Environment}|{SourceContext:l}] {Message}{NewLine}{Exception}";

        // Shared, UI-bindable buffer feeding the in-app log viewer (this process's events).
        var logStore = new LogStore();

        // Serilog, configured as the worker had it — notably Microsoft -> Warning.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(outputTemplate: logOutputTemplate)
            .WriteTo.File(
                Path.Combine(dataDir, "Logs", "log-.txt"),
                outputTemplate: logOutputTemplate,
                rollingInterval: RollingInterval.Day
            )
            .WriteTo.Sink(new InMemoryLogSink(logStore))
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // The desktop app's implementations, registered before the engine so its TryAdd defaults are skipped.
        builder.Services.AddSingleton<AppStateApiKeyStore>();
        builder.Services.AddSingleton<IGw2ApiKeyProvider>(sp => sp.GetRequiredService<AppStateApiKeyStore>());

        // Notifications fan out through a composite: persist to the shared table (+ in-app feed) and
        // fire a Windows toast. NotificationWatcher surfaces the worker's cross-process notifications.
        builder.Services.AddSingleton<NotificationHub>();
        builder.Services.AddSingleton<DbNotifier>();
        builder.Services.AddSingleton<ToastNotifier>();
        builder.Services.AddSingleton<INotifier>(sp => new CompositeNotifier(
            sp.GetRequiredService<DbNotifier>(),
            sp.GetRequiredService<ToastNotifier>()
        ));
        builder.Services.AddHostedService<NotificationWatcher>();

        // Event reminders + Events-screen state: the per-event opt-in store, the favorites (pin-to-top) store,
        // the configurable lead-time setting, and the scheduler that toasts a subscribed event before it begins.
        builder.Services.AddSingleton<EventSubscriptionStore>();
        builder.Services.AddSingleton<EventFavoritesStore>();
        builder.Services.AddSingleton<ReminderSettingsStore>();
        builder.Services.AddHostedService<EventReminderService>();

        // In-app log viewer: this process's events (the sink above) + the worker's (tailed file).
        builder.Services.AddSingleton(logStore);
        builder.Services.AddHostedService<WorkerLogTailer>();

        // UI shell: the navigation page provider, the window, pages, and their view-models.
        builder.Services.AddSingleton<INavigationViewPageProvider, PageProvider>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddSingleton<NotificationsViewModel>();
        // Singleton so its one-second countdown clock keeps ticking and the event list is built once.
        builder.Services.AddSingleton<EventsViewModel>();
        builder.Services.AddSingleton<LogsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        // Transient so the grid re-reads the worker's latest market snapshot on every navigation.
        builder.Services.AddTransient<MarketViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<NotificationsPage>();
        // Cached: the singleton VM owns the live clock, so the page is a thin re-attached view.
        builder.Services.AddSingleton<EventsPage>();
        builder.Services.AddTransient<MarketPage>();
        // Cached so its heavy live list is built once, not rebuilt on every navigation.
        builder.Services.AddSingleton<LogsPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Make the available DB providers selectable; the active one is chosen at launch via Database:Provider
        // (default sqlite). Adding another = reference its project + one more AddGw2GizmosXxx() here.
        builder.Services.AddGw2GizmosSqlite();

        // Provider + connection string come from launch config (--Database:Provider / env). SQLite needs no
        // connection string — default to the per-user data file; any server provider must supply one.
        string? configuredProvider = builder.Configuration["Database:Provider"];
        string provider = string.IsNullOrWhiteSpace(configuredProvider)
            ? ActiveDbProvider.DefaultProviderKey
            : configuredProvider;

        string? configuredConnectionString = builder.Configuration.GetConnectionString("Gw2GizmosDb");
        string connectionString = !string.IsNullOrEmpty(configuredConnectionString)
            ? configuredConnectionString
            : string.Equals(provider, ActiveDbProvider.DefaultProviderKey, StringComparison.OrdinalIgnoreCase)
                ? $"Data Source={dbPath}"
                : throw new InvalidOperationException(
                    $"Database:Provider='{provider}' requires a connection string (ConnectionStrings:Gw2GizmosDb=...).");

        // Only the lightweight delivery poller runs in the UI process; ingestion is the worker's job.
        builder.Services.AddGw2GizmosDeliveryNotifications(connectionString);

        return (builder.Build(), connectionString, provider);
    }

    private void StartWorkerProcess(string dataDir, string connectionString, string provider)
    {
        // The worker ships as a sibling exe in the app's own directory, sharing the runtime + common
        // dependencies (one copy each) — see the CopyWorkerCli/PublishWorkerCli targets in the csproj.
        string workerExe = Path.Combine(AppContext.BaseDirectory, "Gw2Gizmos.Data.Worker.Cli.exe");
        if (!File.Exists(workerExe))
        {
            Log.Warning("Worker process not found at {Path}; data ingestion will not run.", workerExe);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = workerExe,
            WorkingDirectory = dataDir,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // Forward the same provider + connection string so the worker opens the identical database.
        startInfo.ArgumentList.Add($"--ConnectionStrings:Gw2GizmosDb={connectionString}");
        startInfo.ArgumentList.Add($"--Database:Provider={provider}");

        _workerProcess = Process.Start(startInfo);

        // Bind the worker to a kill-on-close job so it can never outlive the app (even on a crash).
        if (_workerProcess is not null)
        {
            _workerJob.AddProcess(_workerProcess);
            Log.Information("Started ingestion worker process (pid {Pid}).", _workerProcess.Id);
        }
    }

    private void SetupTrayIcon(string tooltipText)
    {
        var showItem = new MenuItem { Header = "Show Gw2Gizmos" };
        showItem.Click += (_, _) => ShowWindow();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            _isExiting = true;
            _trayIcon?.Unregister();
            Shutdown();
        };

        var menu = new ContextMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            TooltipText = tooltipText,
            Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/gw2gizmos.png")),
            Menu = menu,
            FocusOnLeftClick = false,
            MenuOnRightClick = true,
        };
        _trayIcon.LeftClick += OnTrayLeftClick;
        _trayIcon.Register();
    }

    // Signature mirrors WPF-UI's RoutedNotifyIconEvent (its sender is [NotNull]); matching the annotation
    // keeps the conversion warning-free without a suppression.
    private void OnTrayLeftClick([NotNull] NotifyIcon sender, RoutedEventArgs e) => ShowWindow();

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Released as the handle closes, freeing the single-instance slot for the next launch.
        _singleInstanceMutex?.Dispose();
        _trayIcon?.Dispose();

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

        // Closing the job is the backstop that guarantees the worker is gone.
        _workerJob.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }
}
