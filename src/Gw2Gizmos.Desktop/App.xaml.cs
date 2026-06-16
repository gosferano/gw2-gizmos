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
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Velopack;
using Velopack.Sources;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
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

    /// <summary>The shell's navigation view, set by <c>MainWindow</c>; lets cards/breadcrumbs navigate.</summary>
    internal static Wpf.Ui.Controls.NavigationView? MainNavigation { get; set; }

    /// <summary>Navigates the shell to a page type (used by Account cards and breadcrumbs).</summary>
    public static void NavigateTo(Type pageType) => MainNavigation?.Navigate(pageType);

    private NotifyIcon? _trayIcon;
    private MainWindow? _window;
    private IHost? _host;
    private Process? _workerProcess;
    private readonly ChildProcessJob _workerJob = new();
    private Mutex? _singleInstanceMutex;
    private bool _isExiting;

    // A development build (Debug) keeps its own per-user data folder and single-instance guard so it can run
    // side by side with the installed release — without sharing its database or tripping its single-instance
    // mutex. Release builds use the shared names so the installer and auto-update see one app.
#if DEBUG
    private const string AppDataFolderName = "Gw2Gizmos.Dev";
    private const string SingleInstanceMutexName = @"Local\Gw2Gizmos.Dev.SingleInstance";
#else
    private const string AppDataFolderName = "Gw2Gizmos";
    private const string SingleInstanceMutexName = @"Local\Gw2Gizmos.SingleInstance";
#endif

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Velopack's hooks run in Program.Main (before WPF starts); here we just begin a normal launch.
        base.OnStartup(e);

        // One instance per user: a second launch (Start-menu + tray, or a Velopack post-update relaunch
        // racing the old process) would spawn a duplicate worker and double every toast. Bail if held.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isPrimary);
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
            AppDataFolderName
        );
        Directory.CreateDirectory(dataDir);
        string dbPath = Path.Combine(dataDir, "gw2gizmos.sqlite");

        // Random, ephemeral name for the local pipe that serves the live worker config (keys + features +
        // intervals) to the worker. Passed to the worker at spawn so it can fetch config without reading the
        // desktop's secret file.
        string workerConfigPipeName = $"Gw2Gizmos.Config.{Guid.NewGuid():N}";

        (IHost host, string connectionString, string dbProvider) = BuildHost(dataDir, dbPath, args, workerConfigPipeName);
        _host = host;
        // The worker owns the database and is its sole migrator/writer; the desktop opens it read-only, so it
        // does not migrate here. The worker (started just below) creates and brings the schema up to date.
        await _host.StartAsync();

        Icons = new IconProvider(_host.Services.GetRequiredService<IServiceScopeFactory>(), dataDir);

        // The worker shares the same database (same provider + connection string) and fetches its config from
        // the config service over the pipe whose name we pass here.
        StartWorkerProcess(dataDir, connectionString, dbProvider, workerConfigPipeName);

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

        // Launched at Windows startup (--minimized): come up in the tray, not on screen. Show then hide off the
        // taskbar so the window handle exists (theming below needs it) without a visible flash.
        if (args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
        {
            _window.ShowInTaskbar = false;
            _window.WindowState = WindowState.Minimized;
            _window.Show();
            _window.Hide();
            _window.ShowInTaskbar = true;
            _window.WindowState = WindowState.Normal;
        }
        else
        {
            _window.Show();
        }

        // Follow the OS *app* theme. WPF-UI's own system-theme detection is unreliable here (it reports Light
        // while the OS is Dark), so read AppsUseLightTheme directly and re-apply when the user switches themes.
        // Done after Show so the window handle exists and the title bar + backdrop get themed too.
        ApplyWindowsTheme();
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        SetupTrayIcon(appTitle);

        // Check for updates in the background; no-op when run from bin (not Velopack-installed).
        _ = CheckForUpdatesAsync(updateManager, _host.Services.GetRequiredService<UpdateStatus>());
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

    /// <summary>Applies the current Windows app theme (light/dark) to the WPF-UI app + window.</summary>
    private void ApplyWindowsTheme()
    {
        ApplicationThemeManager.ApplySystemTheme();
        Log.Information("Applied {Theme} theme.", ApplicationThemeManager.GetAppTheme());
    }

    private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        // The General category fires when the light/dark theme is toggled.
        if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
        {
            Dispatcher.Invoke(ApplyWindowsTheme);
        }
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

    private static async Task CheckForUpdatesAsync(UpdateManager updateManager, UpdateStatus updateStatus)
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
                // Surface it in the UI (the dashboard's App card) — the staged update applies on next restart,
                // or immediately via the card's "Restart now" button.
                updateStatus.SetPending(updateManager, update);
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
        string[] args,
        string workerConfigPipeName
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

        // The desktop owns its own state as per-user files (the worker owns the ingestion DB and the desktop
        // opens it read-only). These are registered before the engine so its TryAdd defaults are skipped.
        builder.Services.AddSingleton(new AppPaths(dataDir));
        // "Launch at Windows startup" registration (HKCU Run); per-build value name so dev/release don't collide.
        builder.Services.AddSingleton(new StartupRegistration(AppDataFolderName));
        // Shared app-update state — set by the startup update check, read by the dashboard's App card.
        builder.Services.AddSingleton<UpdateStatus>();
        // Per-sync trigger generations: bumped when the user enables a feature or adds a key, so the worker syncs
        // that data immediately. Registered first — the key/feature stores bump it.
        builder.Services.AddSingleton<SyncTriggerStore>();
        // Queues user "delete stored data" requests; shipped to the worker (sole DB writer) over the config pipe.
        builder.Services.AddSingleton<DeleteRequestStore>();
        builder.Services.AddSingleton<FileGw2ApiKeyStore>();
        builder.Services.AddSingleton<IGw2ApiKeyProvider>(sp => sp.GetRequiredService<FileGw2ApiKeyStore>());
        // Worker config the desktop owns (source of truth, persisted here) and pushes to the worker over the
        // config pipe: feature on/off toggles, per-sync intervals, and the trigger generations.
        builder.Services.AddSingleton<FeatureSettingsStore>();
        builder.Services.AddSingleton<IntervalSettingsStore>();
        // Serve keys + features + intervals to the cross-platform worker over a local pipe (it never reads our
        // secret file).
        builder.Services.AddHostedService(sp => new WorkerConfigHost(
            workerConfigPipeName,
            sp.GetRequiredService<FileGw2ApiKeyStore>(),
            sp.GetRequiredService<FeatureSettingsStore>(),
            sp.GetRequiredService<IntervalSettingsStore>(),
            sp.GetRequiredService<SyncTriggerStore>(),
            sp.GetRequiredService<DeleteRequestStore>(),
            sp.GetRequiredService<ILogger<WorkerConfigHost>>()
        ));
        // The in-process delivery poller persists its baseline to a file rather than the worker-owned DB.
        builder.Services.AddSingleton<IDeliveryBaselineStore, FileDeliveryBaselineStore>();

        // Notifications surface only as Windows toasts (no in-app feed). A per-category global toggle on the
        // Notifications page can silence a category; the dispatcher checks it before firing.
        builder.Services.AddSingleton<NotificationSettingsStore>();
        builder.Services.AddSingleton<INotifier, NotificationDispatcher>();

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
        // Transient so the page re-reads the stored keys (and current feature state) on every navigation.
        builder.Services.AddTransient<ApiKeysViewModel>();
        // Transient so the page reflects the current toggle/permission state on every navigation.
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<StoredDataViewModel>();
        builder.Services.AddTransient<StoredDataPage>();
        builder.Services.AddTransient<AdvancedSettingsViewModel>();
        // Transient so the grid re-reads the worker's latest item/market data on every navigation.
        builder.Services.AddTransient<ItemsViewModel>();
        // Account: a shared read-only reader; each VM is transient so a section reloads fresh on navigation.
        builder.Services.AddSingleton<AccountReader>();
        // Carries the drilled-into account across the parameterless WPF-UI navigation.
        builder.Services.AddSingleton<SelectedAccountService>();
        builder.Services.AddTransient<AccountViewModel>();
        builder.Services.AddTransient<WalletViewModel>();
        builder.Services.AddTransient<MaterialStorageViewModel>();
        builder.Services.AddTransient<BankViewModel>();
        builder.Services.AddTransient<SharedInventoryViewModel>();
        // Characters: the drilled-into character (transient nav context), the hub + per-character VMs.
        builder.Services.AddSingleton<SelectedCharacterService>();
        builder.Services.AddTransient<CharactersViewModel>();
        builder.Services.AddTransient<CharacterViewModel>();
        builder.Services.AddTransient<CharacterInventoryViewModel>();
        // Sessions: the drilled-into session/segment (transient nav context), the hub + per-session/segment VMs.
        builder.Services.AddSingleton<SelectedSessionService>();
        builder.Services.AddTransient<SessionsViewModel>();
        builder.Services.AddTransient<SessionViewModel>();
        builder.Services.AddTransient<SessionLootViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<NotificationsPage>();
        // Cached: the singleton VM owns the live clock, so the page is a thin re-attached view.
        builder.Services.AddSingleton<EventsPage>();
        builder.Services.AddTransient<ItemsPage>();
        builder.Services.AddTransient<AccountPage>();
        builder.Services.AddTransient<WalletPage>();
        builder.Services.AddTransient<MaterialStoragePage>();
        builder.Services.AddTransient<BankPage>();
        builder.Services.AddTransient<SharedInventoryPage>();
        builder.Services.AddTransient<CharactersPage>();
        builder.Services.AddTransient<CharacterPage>();
        builder.Services.AddTransient<CharacterInventoryPage>();
        builder.Services.AddTransient<SessionsPage>();
        builder.Services.AddTransient<SessionPage>();
        builder.Services.AddTransient<SessionLootPage>();
        // Cached so its heavy live list is built once, not rebuilt on every navigation.
        builder.Services.AddSingleton<LogsPage>();
        builder.Services.AddTransient<ApiKeysPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AdvancedSettingsPage>();

        // Make the available DB providers selectable; the active one is chosen at launch via Database:Provider
        // (default sqlite). Adding another = reference its project + one more AddGw2GizmosXxx() here.
        builder.Services.AddGw2GizmosSqlite();

        // Provider + connection string come from launch config (--Database:Provider / env). SQLite needs no
        // connection string — default to the per-user data file; any server provider must supply one.
        string? configuredProvider = builder.Configuration["Database:Provider"];
        string provider = string.IsNullOrWhiteSpace(configuredProvider)
            ? ActiveDbProvider.DefaultProviderKey
            : configuredProvider;

        bool isDefaultSqlite =
            string.Equals(provider, ActiveDbProvider.DefaultProviderKey, StringComparison.OrdinalIgnoreCase);

        string? configuredConnectionString = builder.Configuration.GetConnectionString("Gw2GizmosDb");

        // The worker is the sole writer, so it gets a read-write connection string (the one we also forward
        // to the worker process).
        string connectionString = !string.IsNullOrEmpty(configuredConnectionString)
            ? configuredConnectionString
            : isDefaultSqlite
                ? $"Data Source={dbPath}"
                : throw new InvalidOperationException(
                    $"Database:Provider='{provider}' requires a connection string (ConnectionStrings:Gw2GizmosDb=...).");

        // The desktop never writes the database — it opens it read-only. For the default per-user SQLite file
        // we append Mode=ReadOnly (we construct that string ourselves, so no flag collision); a custom or
        // server connection string is used as supplied (read-only is enforced by its own credentials).
        string desktopConnectionString = isDefaultSqlite && string.IsNullOrEmpty(configuredConnectionString)
            ? $"{connectionString};Mode=ReadOnly"
            : connectionString;

        // Only the lightweight delivery poller runs in the UI process; ingestion is the worker's job.
        builder.Services.AddGw2GizmosDeliveryNotifications(desktopConnectionString);

        return (builder.Build(), connectionString, provider);
    }

    private void StartWorkerProcess(string dataDir, string connectionString, string provider, string workerConfigPipeName)
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
        // Forward the same provider + connection string so the worker opens the identical database, plus the
        // config pipe name so it can fetch keys/features/intervals live (no secret ever touches the command line).
        startInfo.ArgumentList.Add($"--ConnectionStrings:Gw2GizmosDb={connectionString}");
        startInfo.ArgumentList.Add($"--Database:Provider={provider}");
        startInfo.ArgumentList.Add($"--WorkerConfig:PipeName={workerConfigPipeName}");

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
        // Restore a minimized window, but don't touch a maximized (or normally-resized) one — forcing Normal
        // unconditionally would shrink a maximized window back to its initial size on every reopen.
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

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
