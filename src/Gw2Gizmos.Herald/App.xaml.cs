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

namespace Gw2Gizmos.Herald;

/// <summary>
/// Application entry point. Runs as a tray app: hosts the GW2 data-ingestion engine (with Windows
/// toasts and the user-entered API key wired in) plus a window that hides to the system tray on close.
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _window;
    private IHost? _host;
    private bool _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ToastService.RegisterAppId();

        _host = BuildHost();
        _host.Services.MigrateGw2GizmosDb();
        await _host.StartAsync();

        var apiKeyStore = _host.Services.GetRequiredService<HeraldApiKeyStore>();

        _window = new MainWindow(apiKeyStore);
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
    }

    private static IHost BuildHost()
    {
        string dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gw2Gizmos"
        );
        Directory.CreateDirectory(dataDir);
        string dbPath = Path.Combine(dataDir, "herald.sqlite");

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        string environment = builder.Environment.EnvironmentName.ToLowerInvariant();

        // Serilog, configured as the worker had it — notably Microsoft -> Warning, which keeps the
        // EF Core SQL firehose out of the logs during ingestion.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{Environment}|{SourceContext:l}] {Message}{NewLine}{Exception}"
            )
            .WriteTo.File(Path.Combine(dataDir, "Logs", "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Herald's implementations, registered before the engine so its TryAdd defaults are skipped.
        builder.Services.AddSingleton<HeraldApiKeyStore>();
        builder.Services.AddSingleton<IGw2ApiKeyProvider>(sp => sp.GetRequiredService<HeraldApiKeyStore>());
        builder.Services.AddSingleton<INotifier, ToastNotifier>();

        builder.Services.AddGw2GizmosDataWorker($"Data Source={dbPath}");

        return builder.Build();
    }

    private void SetupTrayIcon()
    {
        var showItem = new MenuItem { Header = "Show Herald" };
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
            ToolTipText = "Gw2Gizmos Herald",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/herald.png")),
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

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
