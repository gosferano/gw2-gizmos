using System.Diagnostics;
using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Http;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string environment = builder.Environment.EnvironmentName.ToLowerInvariant();

// Configure Serilog
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
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder
    .Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

string? connectionString = builder.Configuration.GetConnectionString("Gw2GizmosDb");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'Gw2GizmosDb' is not configured.");
}

// Background services
builder.Services.AddHostedService<Worker>();

// Updaters
builder.Services.AddScoped<ItemsUpdater>();
builder.Services.AddScoped<CommerceUpdater>();
builder.Services.AddScoped<RecipesUpdater>();

// Item added and missing updaters
builder.Services.AddHostedService<ItemsAddedUpdater>();
builder.Services.AddHostedService<ItemsMissingUpdater>();

// Queues
builder.Services.AddSingleton(Channel.CreateUnbounded<ItemAddedDto>());
builder.Services.AddSingleton(Channel.CreateUnbounded<ItemMissingDto>());

// Gw2ApiClient
builder
    .Services.AddHttpClient("Gw2Api")
    .AddPolicyHandler(Policies.GetRetryPolicy())
    .AddPolicyHandler(Policies.GetTimeoutPolicy());
builder.Services.AddSingleton<IGw2ApiClientFactory, Gw2ApiClientFactory>();

// Entity Framework Core
builder.Services.AddDbContext<Gw2GizmosDbContext>(
    options =>
    {
        bool enableSensitiveDataLogging = Debugger.IsAttached;
        options
            .UseSqlite(connectionString)
            .EnableSensitiveDataLogging(enableSensitiveDataLogging)
            .EnableDetailedErrors(enableSensitiveDataLogging)
            .UseLoggerFactory(LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog()));
    },
    contextLifetime: ServiceLifetime.Scoped,
    optionsLifetime: ServiceLifetime.Singleton
);

IHost host = builder.Build();

using (IServiceScope scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
    db.Database.Migrate();
}

host.Run();
