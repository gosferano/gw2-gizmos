using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Velopack;
using Velopack.Sources;
using Wpf.Ui.Abstractions;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Application entry point. Runs as a tray app that hosts the lightweight delivery-notification
/// poller (with Windows toasts) in-process, and launches the heavy ingestion engine as a separate
/// worker process so it can't stall the UI. Both share the same database and API key.
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _window;
    private IHost? _host;
    private Process? _workerProcess;
    private readonly ChildProcessJob _workerJob = new();
    private bool _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Must run before anything else: processes Velopack install/update/uninstall hooks (and exits
        // for those), then returns for a normal launch.
        VelopackApp.Build().Run();

        base.OnStartup(e);

        ToastService.RegisterAppId();

        string dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gw2Gizmos"
        );
        Directory.CreateDirectory(dataDir);
        string dbPath = Path.Combine(dataDir, "herald.sqlite");

        _host = BuildHost(dataDir, dbPath);
        _host.Services.MigrateGw2GizmosDb();
        await _host.StartAsync();

        StartWorkerProcess(dataDir, dbPath);

        _window = _host.Services.GetRequiredService<MainWindow>();
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

        SetupTrayIcon();

        // Check for updates in the background; no-op when run from bin (not Velopack-installed).
        _ = CheckForUpdatesAsync();
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateManager = new UpdateManager(
                new GithubSource("https://github.com/gosferano/gw2-gizmos", null, prerelease: false)
            );

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

    private static IHost BuildHost(string dataDir, string dbPath)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

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

        // In-app log viewer: this process's events (the sink above) + the worker's (tailed file).
        builder.Services.AddSingleton(logStore);
        builder.Services.AddHostedService<WorkerLogTailer>();

        // UI shell: the navigation page provider, the window, pages, and their view-models.
        builder.Services.AddSingleton<INavigationViewPageProvider, PageProvider>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddSingleton<NotificationsViewModel>();
        builder.Services.AddSingleton<LogsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<NotificationsPage>();
        // Cached so its heavy live list is built once, not rebuilt on every navigation.
        builder.Services.AddSingleton<LogsPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Only the lightweight delivery poller runs in the UI process; ingestion is the worker's job.
        builder.Services.AddGw2GizmosDeliveryNotifications($"Data Source={dbPath}");

        return builder.Build();
    }

    private void StartWorkerProcess(string dataDir, string dbPath)
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
        startInfo.ArgumentList.Add($"--ConnectionStrings:Gw2GizmosDb=Data Source={dbPath}");

        _workerProcess = Process.Start(startInfo);

        // Bind the worker to a kill-on-close job so it can never outlive the app (even on a crash).
        if (_workerProcess is not null)
        {
            _workerJob.AddProcess(_workerProcess);
            Log.Information("Started ingestion worker process (pid {Pid}).", _workerProcess.Id);
        }
    }

    private void SetupTrayIcon()
    {
        var showItem = new MenuItem { Header = "Show Gw2Gizmos" };
        showItem.Click += (_, _) => ShowWindow();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            _isExiting = true;
            _trayIcon?.Dispose();
            Shutdown();
        };

        var menu = new ContextMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(exitItem);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Gw2Gizmos",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/gw2gizmos.png")),
            ContextMenu = menu,
        };
        _trayIcon.TrayLeftMouseDown += (_, _) => ShowWindow();
    }

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
