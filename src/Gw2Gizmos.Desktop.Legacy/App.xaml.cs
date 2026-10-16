using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;
using Gw2Gizmos.Desktop.Platform.Windows;
using Gw2Gizmos.Desktop.Legacy.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Tray.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

/// <summary>
/// Application entry point. Runs as a tray app that hosts the lightweight delivery-notification
/// poller (with Windows toasts) in-process, and launches the heavy ingestion engine as a separate
/// worker process so it can't stall the UI. Both share the same database and API key.
/// </summary>
public partial class App : Application
{
    /// <summary>Item-icon cache, exposed statically so the lightweight <c>ItemImage</c> control can reach it.</summary>
    public static IconProvider<ImageSource>? Icons { get; private set; }

    /// <summary>The shell's navigation view, set by <c>MainWindow</c>; lets cards/breadcrumbs navigate.</summary>
    internal static Wpf.Ui.Controls.NavigationView? MainNavigation { get; set; }

    /// <summary>Navigates the shell to a page type (used by Account cards and breadcrumbs).</summary>
    public static void NavigateTo(Type pageType) => MainNavigation?.Navigate(pageType);

    /// <summary>
    /// Highlights the rail item a page belongs under, so a section stays selected while you're on its sub-pages.
    /// The section is the page's root-breadcrumb <c>Target</c> (every sub-page already declares it); top-level pages
    /// pass null and fall back to the rail's own current selection. Called by <c>PageShell</c> on load, so any new
    /// page or hierarchy works with no extra wiring or mapping.
    /// </summary>
    public static void HighlightNavSection(AppPage? breadcrumbRoot)
    {
        if (MainNavigation is null)
        {
            return;
        }

        Type? section = breadcrumbRoot is { } root
            ? WpfNavigationService.PageTypeFor(root)
            : (MainNavigation.SelectedItem as Wpf.Ui.Controls.NavigationViewItem)?.TargetPageType;
        if (section is null)
        {
            return;
        }

        foreach (Wpf.Ui.Controls.NavigationViewItem item in MainNavigation.MenuItems
                     .OfType<Wpf.Ui.Controls.NavigationViewItem>()
                     .Concat(MainNavigation.FooterMenuItems.OfType<Wpf.Ui.Controls.NavigationViewItem>()))
        {
            item.SetValue(Wpf.Ui.Controls.NavigationViewItem.IsActiveProperty, item.TargetPageType == section);
        }
    }

    // --- Back navigation: WPF-UI journals the page sequence, but it recreates each page's view-model on back,
    // which would re-read the *current* selection singletons. So we snapshot the selection per visited page and
    // restore the previous one just before each back, so a detail page shows the item it originally did. ---
    private static IServiceProvider? _services;
    private static readonly List<NavSelection> _trail = new();
    private static bool _inBack;

    private readonly record struct NavSelection(
        string? AccountId, string? AccountName,
        long? SessionId, string SessionTitle, long? SegmentId, string SegmentTitle,
        string? Character);

    /// <summary>Records the selection of the page just navigated to (skipped while going back). Call on Navigated.</summary>
    internal static void OnNavigated()
    {
        if (_inBack)
        {
            _inBack = false;
        }
        else if (_services is not null)
        {
            _trail.Add(Capture());
        }
    }

    /// <summary>Back via Alt+Left / mouse: restore the previous page's selection, then walk WPF-UI's journal so the
    /// rebuilt page shows the item it originally did.</summary>
    public static void GoBack()
    {
        if (MainNavigation is not { CanGoBack: true } || _trail.Count < 2)
        {
            return;
        }

        _trail.RemoveAt(_trail.Count - 1);
        _inBack = true; // skip the capture from the back navigation below
        Apply(_trail[^1]);
        MainNavigation.GoBack();
    }

    private static NavSelection Capture()
    {
        IServiceProvider services = _services!;
        var account = services.GetRequiredService<SelectedAccountService>();
        var session = services.GetRequiredService<SelectedSessionService>();
        var character = services.GetRequiredService<SelectedCharacterService>();
        return new NavSelection(
            account.AccountId, account.AccountName,
            session.GameSessionId, session.GameSessionTitle, session.SegmentId, session.SegmentTitle,
            character.CharacterName);
    }

    private static void Apply(NavSelection s)
    {
        IServiceProvider services = _services!;
        var account = services.GetRequiredService<SelectedAccountService>();
        // Only re-select the account when it actually differs (its setter persists + fires change events).
        if (s.AccountId is not null && !string.Equals(account.AccountId, s.AccountId, StringComparison.OrdinalIgnoreCase))
        {
            account.Select(s.AccountId, s.AccountName ?? "");
        }

        var session = services.GetRequiredService<SelectedSessionService>();
        if (s.SessionId is { } sessionId)
        {
            session.SelectSession(sessionId, s.SessionTitle);
        }

        if (s.SegmentId is { } segmentId)
        {
            // Restored navigation state doesn't carry the segment's live status; treat it as a normal (deletable)
            // segment. The active segment is a transient, in-progress thing that isn't deep-linked to in practice.
            session.SelectSegment(segmentId, s.SegmentTitle, isActive: false);
        }

        if (s.Character is { } character)
        {
            services.GetRequiredService<SelectedCharacterService>().Select(character);
        }
    }

    private NotifyIcon? _trayIcon;
    private MainWindow? _window;
    private IHost? _host;
    private WorkerSupervisor? _worker;
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

        _host = BuildHost(dataDir, dbPath, args, workerConfigPipeName);
        // The worker owns the database and is its sole migrator/writer; the desktop opens it read-only, so it
        // does not migrate here. The worker (started just below) creates and brings the schema up to date.
        await _host.StartAsync();

        Icons = new IconProvider<ImageSource>(
            _host.Services.GetRequiredService<IServiceScopeFactory>(), dataDir, WpfIconDecoder.Decode);
        _services = _host.Services; // for back-navigation selection snapshots

        // The worker shares the same database (same provider + connection string) and fetches its config from
        // the config service over the pipe whose name it's given.
        StartWorkerProcess();

        // A dev build (run from bin) isn't Velopack-installed; suffix its window title + tray tooltip so
        // it's distinguishable from the installed app when both are running at once. (The update check itself
        // runs as a hosted service, started with the host above.)
        bool isInstalled = _host.Services.GetRequiredService<AppUpdateService>().IsInstalled;
        string appTitle = isInstalled ? "Gw2Gizmos" : "Gw2Gizmos (development)";

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
        ApplyPageBackgroundOverride();
        Log.Information("Applied {Theme} theme.", ApplicationThemeManager.GetAppTheme());
    }

    /// <summary>
    /// WPF-UI's light <c>ApplicationBackgroundBrush</c> is near-white, which reads too bright behind the cards.
    /// Override it with a softer grey in light theme (so cards stand out on a calmer surface); leave dark theme on
    /// the WPF-UI default. The window grid binds this brush dynamically, so it updates on a live theme switch too.
    /// </summary>
    private static void ApplyPageBackgroundOverride()
    {
        const string key = "ApplicationBackgroundBrush";
        if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Light)
        {
            var brush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xE6, 0xE6, 0xE6));
            brush.Freeze();
            Current.Resources[key] = brush;
        }
        else
        {
            // Drop our override so the WPF-UI dark theme's own background applies.
            Current.Resources.Remove(key);
        }
    }

    private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        // The General category fires when the light/dark theme is toggled.
        if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
        {
            Dispatcher.Invoke(ApplyWindowsTheme);
        }
    }

    private static IHost BuildHost(
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

        // Shared, UI-bindable buffer feeding the in-app log viewer (this process's events). It exists before the
        // host (Serilog needs it), so it gets the dispatcher directly; the same instance is registered below.
        var uiDispatcher = new WpfUiDispatcher();
        var logStore = new LogStore(uiDispatcher);

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

        // Provider + connection strings come from launch config (--Database:Provider / env): read-write for the
        // worker (forwarded to its process), read-only for the desktop.
        var database = DesktopDatabaseSettings.Resolve(builder.Configuration, dbPath);

        // Everything shared with other UI heads: stores, the worker-config pipe, reminders, the log feed, the
        // view-models, and the in-process delivery poller.
        builder.Services.AddGw2GizmosDesktopCore(new AppPaths(dataDir), logStore, workerConfigPipeName, database);

        // The Windows platform services: "launch at Windows startup" (HKCU Run; per-build value name so dev/release
        // don't collide), toasts, DPAPI for the API keys at rest, and the kill-on-close job the worker runs in.
        builder.Services.AddGw2GizmosWindowsPlatform(AppDataFolderName);

        // The WPF implementations of the UI seams the shared view-models are written against.
        builder.Services.AddSingleton<IUiDispatcher>(uiDispatcher);
        builder.Services.AddSingleton<IItemsViewFactory, WpfItemsViewFactory>();
        builder.Services.AddSingleton<IDialogService, WpfDialogService>();
        builder.Services.AddSingleton<IClipboardService, WpfClipboardService>();
        builder.Services.AddSingleton<INavigationService, WpfNavigationService>();

        // UI shell: the navigation page provider, the window, and the pages.
        builder.Services.AddSingleton<INavigationViewPageProvider, PageProvider>();
        builder.Services.AddSingleton<MainWindow>();
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
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AdvancedSettingsPage>();
        builder.Services.AddTransient<StoredDataPage>();

        // Make the available DB providers selectable; the active one is chosen at launch via Database:Provider
        // (default sqlite). Adding another = reference its project + one more AddGw2GizmosXxx() here.
        builder.Services.AddGw2GizmosSqlite();

        return builder.Build();
    }

    private void StartWorkerProcess()
    {
        // The platform services bind each worker (the first and any restart) to a kill-on-close job, so it can never
        // outlive the app, even on a crash.
        _worker = _host!.Services.GetRequiredService<WorkerSupervisor>();
        _worker.Start();
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

        _worker?.Stop();

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }
}
