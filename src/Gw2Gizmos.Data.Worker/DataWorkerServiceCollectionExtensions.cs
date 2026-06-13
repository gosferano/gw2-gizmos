using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Composes the GW2 data engine into a host. The work splits by weight so a UI host and a background
/// host can take different pieces:
/// <list type="bullet">
/// <item><see cref="AddGw2GizmosIngestion"/> — the heavy bulk sync (items, commerce, recipes; account
/// data later). Belongs in a background process.</item>
/// <item><see cref="AddGw2GizmosDeliveryNotifications"/> — the lightweight trading-post delivery poller.
/// Cheap enough to live in the UI process so notifications need no cross-process plumbing.</item>
/// </list>
/// Each consumer supplies its own logging and <see cref="INotifier"/>; both read the API key through
/// <see cref="IGw2ApiKeyProvider"/> (configuration by default; Gw2Gizmos registers an AppState-backed one).
/// </summary>
public static class DataWorkerServiceCollectionExtensions
{
    /// <summary>The heavy bulk-ingestion engine. Intended for the background worker process.</summary>
    public static IServiceCollection AddGw2GizmosIngestion(this IServiceCollection services, string connectionString)
    {
        AddCore(services, connectionString);

        services.AddHostedService<Worker>();
        services.AddScoped<ItemsUpdater>();
        services.AddScoped<CommerceUpdater>();
        services.AddScoped<CurrenciesUpdater>();
        services.AddScoped<RecipesUpdater>();
        services.AddScoped<MarketUpdater>();
        services.AddScoped<PriceHistoryRetentionUpdater>();
        // Singleton so it keeps the previous poll's totals in memory to compute per-interval volume.
        services.AddSingleton<PriceSnapshotUpdater>();

        // Channel consumers + queues that connect the ingestion updaters.
        services.AddHostedService<ItemsAddedUpdater>();
        services.AddHostedService<ItemsMissingUpdater>();
        services.AddSingleton(Channel.CreateUnbounded<ItemAddedDto>());
        services.AddSingleton(Channel.CreateUnbounded<ItemMissingDto>());

        return services;
    }

    /// <summary>The lightweight trading-post delivery poller. Cheap enough for the UI process.</summary>
    public static IServiceCollection AddGw2GizmosDeliveryNotifications(
        this IServiceCollection services,
        string connectionString
    )
    {
        AddCore(services, connectionString);

        // LogNotifier is the default; a consumer (e.g. Gw2Gizmos) registers its own INotifier first.
        services.TryAddSingleton<INotifier, LogNotifier>();
        services.AddHostedService<CommerceDeliveryUpdater>();

        return services;
    }

    /// <summary>Shared dependencies: the API client, EF Core, and the default API-key provider.</summary>
    private static void AddCore(IServiceCollection services, string connectionString)
    {
        // Configuration-backed key provider is the default; consumers can register their own first.
        services.TryAddSingleton<IGw2ApiKeyProvider, ConfigurationGw2ApiKeyProvider>();

        // Named "Gw2Api" HttpClient + IGw2ApiClientFactory, with built-in retry/429 resilience.
        services.AddGw2ApiClient();

        // The concrete EF provider is chosen at launch (Database:Provider) from those registered at the
        // composition root; this layer stays provider-agnostic. EF picks up the host's ILoggerFactory.
        services.TryAddSingleton<ActiveDbProvider>();
        services.AddDbContext<Gw2GizmosDbContext>(
            (sp, options) => sp.GetRequiredService<ActiveDbProvider>().Provider.Configure(options, connectionString),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton
        );
    }

    /// <summary>
    /// Brings the database up to the current schema and applies provider-specific setup, via the registered
    /// <see cref="IGw2GizmosDbProvider"/>. Call once after building the host.
    /// </summary>
    public static void MigrateGw2GizmosDb(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        scope.ServiceProvider.GetRequiredService<ActiveDbProvider>().Provider.EnsureDatabase(dbContext);
    }
}
