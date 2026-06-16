using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.MumbleLink.Client;
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
        // Per-sync wake signals + the watcher that fires them when the desktop bumps a generation, so an enabled
        // feature or added key syncs within a few seconds rather than at the next tick. Standalone runs get the
        // no-op trigger source (registered in AddCore), so the watcher simply idles.
        services.AddSingleton<SyncTriggers>();
        services.AddHostedService<SyncTriggerWatcher>();
        services.AddScoped<ItemsUpdater>();
        services.AddScoped<CurrenciesUpdater>();
        services.AddScoped<MaterialCategoriesUpdater>();
        services.AddScoped<RecipesUpdater>();
        services.AddScoped<ItemCraftCostUpdater>();
        services.AddScoped<PriceHistoryRetentionUpdater>();
        services.AddScoped<AccountSyncUpdater>();
        // Singleton so it keeps the previous poll's totals in memory to compute per-interval volume.
        services.AddSingleton<PriceSnapshotUpdater>();

        // Serializes the periodic account loop with the session tracker's on-demand boundary syncs.
        services.AddSingleton<AccountSyncGate>();
        // Play-session tracking reads MumbleLink (Windows named shared memory), so it's Windows-only; the rest of
        // the worker stays portable. The OS guard also satisfies the reader's [SupportedOSPlatform] surface.
        if (OperatingSystem.IsWindows())
        {
            services.AddMumbleLink();
            services.AddHostedService<SessionTracker>();
        }

        // Channel consumer + queue that connects the ingestion updaters: ItemsUpdater discovers item ids the
        // catalog is missing and queues them; ItemsMissingUpdater is the single consumer that fetches + upserts.
        services.AddHostedService<ItemsMissingUpdater>();
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
        // Baseline persistence is host-supplied; default to in-memory if the host registers nothing.
        services.TryAddSingleton<IDeliveryBaselineStore, InMemoryDeliveryBaselineStore>();
        services.AddHostedService<CommerceDeliveryUpdater>();

        return services;
    }

    /// <summary>Shared dependencies: the API client, EF Core, and the default API-key provider.</summary>
    private static void AddCore(IServiceCollection services, string connectionString)
    {
        // Configuration-backed key provider is the default; consumers can register their own first.
        services.TryAddSingleton<IGw2ApiKeyProvider, ConfigurationGw2ApiKeyProvider>();
        // Likewise the feature gate: the standalone worker reads Worker:Features:* from config, while a
        // desktop-launched worker registers an IPC-backed gate (and the desktop a settings-backed one) first.
        services.TryAddSingleton<IFeatureGate, ConfigurationFeatureGate>();
        // And the interval gate: standalone reads Worker:Intervals:* from config; the desktop pushes intervals
        // over the pipe (the IPC provider, registered first, satisfies all four gates/sources).
        services.TryAddSingleton<IIntervalGate, ConfigurationIntervalGate>();
        // The sync-trigger source: standalone has no desktop to signal it, so it's a no-op; a desktop-launched
        // worker registers the IPC provider (which carries generations) first.
        services.TryAddSingleton<ISyncTriggerSource, NullSyncTriggerSource>();

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
