using System.Diagnostics;
using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Http;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Composes the GW2 data-ingestion engine (background updaters, the API client, EF Core, and the
/// work queues) into a host. Both the headless CLI and the Herald desktop app build on this; each
/// supplies its own logging, configuration, and <see cref="INotifier"/>.
/// </summary>
public static class DataWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddGw2GizmosDataWorker(this IServiceCollection services, string connectionString)
    {
        // Top-level scheduler that drives the periodic updaters.
        services.AddHostedService<Worker>();

        // Updaters (resolved per-scope by the scheduler / channel consumers).
        services.AddScoped<ItemsUpdater>();
        services.AddScoped<CommerceUpdater>();
        services.AddScoped<CurrenciesUpdater>();
        services.AddScoped<RecipesUpdater>();

        // Channel consumers.
        services.AddHostedService<ItemsAddedUpdater>();
        services.AddHostedService<ItemsMissingUpdater>();

        // Trading-post delivery notifications. LogNotifier and the configuration-backed key provider
        // are defaults; a consumer (e.g. Herald) can register its own INotifier / IGw2ApiKeyProvider
        // before calling this to override them.
        services.TryAddSingleton<INotifier, LogNotifier>();
        services.TryAddSingleton<IGw2ApiKeyProvider, ConfigurationGw2ApiKeyProvider>();
        services.AddHostedService<CommerceDeliveryUpdater>();

        // Work queues.
        services.AddSingleton(Channel.CreateUnbounded<ItemAddedDto>());
        services.AddSingleton(Channel.CreateUnbounded<ItemMissingDto>());

        // GW2 API client.
        services
            .AddHttpClient("Gw2Api")
            .AddPolicyHandler(Policies.GetRetryPolicy())
            .AddPolicyHandler(Policies.GetTimeoutPolicy());
        services.AddSingleton<IGw2ApiClientFactory, Gw2ApiClientFactory>();

        // Entity Framework Core. EF picks up the host's ILoggerFactory automatically.
        services.AddDbContext<Gw2GizmosDbContext>(
            options =>
            {
                bool enableSensitiveDataLogging = Debugger.IsAttached;
                options
                    .UseSqlite(connectionString)
                    .EnableSensitiveDataLogging(enableSensitiveDataLogging)
                    .EnableDetailedErrors(enableSensitiveDataLogging);
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton
        );

        return services;
    }

    /// <summary>Applies any pending EF Core migrations. Call once after building the host.</summary>
    public static void MigrateGw2GizmosDb(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        dbContext.Database.Migrate();
    }
}
