using System.Diagnostics;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

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

// Ingestion uses public endpoints today, so no API key is required; the default configuration-backed key
// provider (env/appsettings) suffices. When account-data sync (planned) needs a key, Gw2Gizmos will pass it
// to this process at launch.

// SQLite is the chosen database provider; register it before the data services so this layer stays
// provider-agnostic. Swapping providers means registering a different one here.
builder.Services.AddGw2GizmosSqlite();
builder.Services.AddGw2GizmosIngestion(connectionString);

IHost host = builder.Build();
host.Services.MigrateGw2GizmosDb();
host.Run();
