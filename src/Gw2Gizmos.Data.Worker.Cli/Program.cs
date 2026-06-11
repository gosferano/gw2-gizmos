using System.Diagnostics;
using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

// Shares the database with Herald. Herald passes the connection string when it launches this
// process: --ConnectionStrings:Gw2GizmosDb="Data Source=...".
string? connectionString = builder.Configuration.GetConnectionString("Gw2GizmosDb");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Gw2GizmosDb' is not configured. Pass --ConnectionStrings:Gw2GizmosDb=\"Data Source=...\"."
    );
}

// The user enters the API key in Herald; it's shared via AppState. Ingestion uses public endpoints
// today, but account-data sync (planned) will authenticate with this key.
builder.Services.AddSingleton<AppStateApiKeyStore>();
builder.Services.AddSingleton<IGw2ApiKeyProvider>(sp => sp.GetRequiredService<AppStateApiKeyStore>());

builder.Services.AddGw2GizmosIngestion(connectionString);

IHost host = builder.Build();
host.Services.MigrateGw2GizmosDb();
host.Run();
