using System.Diagnostics;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Anchor the content root to the exe directory so appsettings.json (the standalone feature defaults) is found
// no matter the working directory — the desktop launches this worker with a different cwd.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

string environment = builder.Environment.EnvironmentName.ToLowerInvariant();
const string logOutputTemplate =
    "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{Environment}|{SourceContext:l}] {Message}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", environment)
    .WriteTo.Console(outputTemplate: logOutputTemplate)
    .WriteTo.File("Logs/worker-.txt", outputTemplate: logOutputTemplate, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// The worker owns this database — it is the sole writer. Gw2Gizmos passes the connection string when it
// launches this process: --ConnectionStrings:Gw2GizmosDb="Data Source=...".
string? connectionString = builder.Configuration.GetConnectionString("Gw2GizmosDb");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Gw2GizmosDb' is not configured. Pass --ConnectionStrings:Gw2GizmosDb=\"Data Source=...\"."
    );
}

// Config source: when Gw2Gizmos spawns this worker it passes --WorkerConfig:PipeName, and we fetch the live
// config (keys + enabled features + intervals) from the desktop over a local pipe (cross-platform, no secret at
// rest here). A standalone worker omits the pipe and falls back to the configuration/env providers
// (GW2_API_KEY / Gw2:ApiKey, Worker:Features:*, Worker:Intervals:*).
string? workerConfigPipeName = builder.Configuration["WorkerConfig:PipeName"];
if (!string.IsNullOrWhiteSpace(workerConfigPipeName))
{
    // One pipe-backed provider serves keys + feature toggles + intervals (all ride the same payload).
    builder.Services.AddSingleton(sp => new IpcWorkerConfigProvider(
        workerConfigPipeName,
        sp.GetRequiredService<ILogger<IpcWorkerConfigProvider>>()
    ));
    builder.Services.AddSingleton<IGw2ApiKeyProvider>(sp => sp.GetRequiredService<IpcWorkerConfigProvider>());
    builder.Services.AddSingleton<IFeatureGate>(sp => sp.GetRequiredService<IpcWorkerConfigProvider>());
    builder.Services.AddSingleton<IIntervalGate>(sp => sp.GetRequiredService<IpcWorkerConfigProvider>());
    builder.Services.AddSingleton<ISyncTriggerSource>(sp => sp.GetRequiredService<IpcWorkerConfigProvider>());
}

// SQLite is the chosen database provider; register it before the data services so this layer stays
// provider-agnostic. Swapping providers means registering a different one here.
builder.Services.AddGw2GizmosSqlite();
builder.Services.AddGw2GizmosIngestion(connectionString);

IHost host = builder.Build();
host.Services.MigrateGw2GizmosDb();
host.Run();
