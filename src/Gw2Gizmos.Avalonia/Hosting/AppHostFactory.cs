using System.Diagnostics;
using Gw2Gizmos.Avalonia.Navigation;
using Gw2Gizmos.Avalonia.Platform.Linux;
using Gw2Gizmos.Avalonia.Ui;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Platform;
using Gw2Gizmos.Desktop.Core.Ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Gw2Gizmos.Avalonia.Hosting;

/// <summary>Builds the app's host: Serilog, the shared Core services, and this head's seam implementations.</summary>
internal static class AppHostFactory
{
    public static IHost Build(string dataDirectory, string[] args, string workerConfigPipeName)
    {
        // Pass args so --Database:Provider / --ConnectionStrings:Gw2GizmosDb (and matching env vars) flow into
        // configuration.
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        string environment = builder.Environment.EnvironmentName.ToLowerInvariant();
        const string logOutputTemplate =
            "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{Environment}|{SourceContext:l}] {Message}{NewLine}{Exception}";

        // Shared, UI-bindable buffer feeding the in-app log viewer (this process's events). It exists before the
        // host (Serilog needs it), so it gets the dispatcher directly; the same instance is registered below.
        var uiDispatcher = new AvaloniaUiDispatcher();
        var logStore = new LogStore(uiDispatcher);

        // Same Serilog setup as the WPF app — notably Microsoft -> Warning.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(outputTemplate: logOutputTemplate)
            .WriteTo.File(
                Path.Combine(dataDirectory, "Logs", "log-.txt"),
                outputTemplate: logOutputTemplate,
                rollingInterval: RollingInterval.Day
            )
            .WriteTo.Sink(new InMemoryLogSink(logStore))
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Provider + connection strings come from launch config: read-write for the worker, read-only for us.
        var database = DesktopDatabaseSettings.Resolve(
            builder.Configuration, Path.Combine(dataDirectory, "gw2gizmos.sqlite"));

        // Platform services. Linux only for now; Windows will reuse the WPF app's implementations once they move to
        // a shared Windows platform library.
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("The Avalonia app currently supports Linux only.");
        }

        // GW2 runs under Wine/Proton here, so its MumbleLink reaches the worker through a bridge-mirrored file.
        string mumbleLinkFilePath = ProtonMumbleLinkBridge.DefaultFilePath(AppDataDirectory.FolderName, dataDirectory);

        // Everything shared with the WPF app: stores, the worker-config pipe + launcher, reminders, update checks,
        // the log feed, the view-models, and the in-process delivery poller.
        builder.Services.AddGw2GizmosDesktopCore(
            new AppPaths(dataDirectory), logStore, workerConfigPipeName, database, mumbleLinkFilePath);

        builder.Services.AddSingleton<IToastService, NotifySendToastService>();
        builder.Services.AddSingleton<IStartupRegistration>(new XdgAutostartRegistration(AppDataDirectory.FolderName));
        builder.Services.AddSingleton<ISecretProtector>(sp => new SecretServiceProtector(
            AppDataDirectory.FolderName, sp.GetRequiredService<ILogger<SecretServiceProtector>>()));
        ProtonMumbleLinkBridge.Register(builder.Services, mumbleLinkFilePath);

        // The Avalonia implementations of the UI seams the shared view-models are written against.
        builder.Services.AddSingleton<IUiDispatcher>(uiDispatcher);
        builder.Services.AddSingleton<IItemsViewFactory, AvaloniaItemsViewFactory>();
        builder.Services.AddSingleton<IDialogService, AvaloniaDialogService>();
        builder.Services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        builder.Services.AddSingleton<AvaloniaNavigationService>();
        builder.Services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<AvaloniaNavigationService>());

        // UI shell: the window and the pages (resolved by the page factory as you navigate).
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<PageFactory>();
        PageFactory.RegisterPages(builder.Services);

        // Make the available DB providers selectable; the active one is chosen at launch via Database:Provider.
        builder.Services.AddGw2GizmosSqlite();

        return builder.Build();
    }
}
