using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Data.Worker.Updaters;

namespace Gw2Gizmos.Data.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFeatureGate _featureGate;
    private readonly IIntervalGate _intervalGate;
    private readonly SyncTriggers _triggers;
    private readonly ILogger<Worker> _logger;

    // Serializes craft-cost refreshes so the 15-minute timer and the after-price trigger can never run
    // two wholesale table replaces at once (which would race on the ItemCraftCosts table).
    private readonly SemaphoreSlim _craftCostRefreshLock = new(1, 1);

    public Worker(
        IServiceScopeFactory scopeFactory,
        IFeatureGate featureGate,
        IIntervalGate intervalGate,
        SyncTriggers triggers,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _featureGate = featureGate;
        _intervalGate = intervalGate;
        _triggers = triggers;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The user-tunable syncs start at their catalog default and are retimed each iteration from the interval
        // gate (so a change on Settings → Advanced applies on the next wait). craft-cost + price-history
        // retention are fixed internal cadences.
        using var currenciesTimer = new PeriodicTimer(WorkerSyncs.Currencies.Default);
        using var materialCategoriesTimer = new PeriodicTimer(WorkerSyncs.MaterialCategories.Default);
        using var itemsTimer = new PeriodicTimer(WorkerSyncs.Items.Default);
        using var recipesTimer = new PeriodicTimer(WorkerSyncs.Recipes.Default);
        // Recompute craft costs every 15 minutes (also refreshed right after each price poll, since they
        // depend on ingredient prices).
        using var craftCostTimer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        using var priceSnapshotTimer = new PeriodicTimer(WorkerSyncs.Prices.Default);
        // Coarsen the accumulating price history hourly so the fine-grained tier stays bounded.
        using var priceHistoryRetentionTimer = new PeriodicTimer(TimeSpan.FromHours(1));
        using var accountSyncTimer = new PeriodicTimer(WorkerSyncs.Account.Default);

        // Run all tasks concurrently
        var tasks = new List<Task>
        {
            RunCurrenciesUpdater(currenciesTimer, stoppingToken),
            RunMaterialCategoriesUpdater(materialCategoriesTimer, stoppingToken),
            RunItemsUpdater(itemsTimer, stoppingToken),
            RunRecipesUpdater(recipesTimer, stoppingToken),
            RunCraftCostUpdater(craftCostTimer, stoppingToken),
            RunPriceSnapshotUpdater(priceSnapshotTimer, stoppingToken),
            RunPriceHistoryRetentionUpdater(priceHistoryRetentionTimer, stoppingToken),
            // The account loop always runs; each section (wallet/materials/bank/shared) is gated per-feature
            // inside the updater, so toggles take effect live without restarting the worker.
            RunAccountSyncUpdater(accountSyncTimer, stoppingToken),
        };

        await Task.WhenAll(tasks);
    }

    private async Task RunCurrenciesUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        SyncTrigger trigger = _triggers.Get(WorkerSyncs.Currencies.Key);
        do
        {
            timer.Period = _intervalGate.GetInterval(WorkerSyncs.Currencies.Key);
            await RunUpdateSafely(
                async scope =>
                {
                    var currenciesUpdater = scope.ServiceProvider.GetRequiredService<CurrenciesUpdater>();
                    await currenciesUpdater.UpdateCurrencies(stoppingToken);
                },
                stoppingToken
            );
        } while (await trigger.WaitForNextRunAsync(timer, stoppingToken));
    }

    private async Task RunMaterialCategoriesUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        SyncTrigger trigger = _triggers.Get(WorkerSyncs.MaterialCategories.Key);
        do
        {
            timer.Period = _intervalGate.GetInterval(WorkerSyncs.MaterialCategories.Key);
            await RunUpdateSafely(
                async scope =>
                {
                    var updater = scope.ServiceProvider.GetRequiredService<MaterialCategoriesUpdater>();
                    await updater.UpdateMaterialCategories(stoppingToken);
                },
                stoppingToken
            );
        } while (await trigger.WaitForNextRunAsync(timer, stoppingToken));
    }

    private async Task RunItemsUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        SyncTrigger trigger = _triggers.Get(WorkerSyncs.Items.Key);
        do
        {
            timer.Period = _intervalGate.GetInterval(WorkerSyncs.Items.Key);
            // Gated by the Item-data feature (on by default); skipping a tick leaves the existing catalog intact.
            if (!_featureGate.IsEnabled(WorkerFeatures.ItemsSync.Key))
            {
                continue;
            }

            await RunUpdateSafely(
                async scope =>
                {
                    var itemsUpdater = scope.ServiceProvider.GetRequiredService<ItemsUpdater>();
                    await itemsUpdater.UpdateItems(stoppingToken);
                },
                stoppingToken
            );
        } while (await trigger.WaitForNextRunAsync(timer, stoppingToken));
    }

    private async Task RunRecipesUpdater(PeriodicTimer recipesTimer, CancellationToken stoppingToken)
    {
        SyncTrigger trigger = _triggers.Get(WorkerSyncs.Recipes.Key);
        do
        {
            recipesTimer.Period = _intervalGate.GetInterval(WorkerSyncs.Recipes.Key);
            // Gated by the Recipes feature (on by default); skipping a tick leaves the existing recipes intact.
            if (!_featureGate.IsEnabled(WorkerFeatures.RecipesSync.Key))
            {
                continue;
            }

            await RunUpdateSafely(
                async scope =>
                {
                    var recipesUpdater = scope.ServiceProvider.GetRequiredService<RecipesUpdater>();
                    await recipesUpdater.UpdateRecipes(stoppingToken);
                },
                stoppingToken
            );
        } while (await trigger.WaitForNextRunAsync(recipesTimer, stoppingToken));
    }

    private async Task RunCraftCostUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RefreshCraftCostsSafely(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Recomputes the craft-cost cache under a lock, so the periodic timer and the after-commerce trigger
    /// never overlap. If a refresh is already running, this awaits it and then runs again — a harmless
    /// back-to-back rebuild rather than a racing one. Gated on <see cref="WorkerFeatures.PricesSync"/>: craft
    /// cost is derived from ingredient prices, so without price history every cost computes to 0 and the run is
    /// wasted — this single gate covers both the timer loop and the after-commerce trigger.
    /// </summary>
    private async Task RefreshCraftCostsSafely(CancellationToken stoppingToken)
    {
        if (!_featureGate.IsEnabled(WorkerFeatures.PricesSync.Key))
        {
            return;
        }

        await _craftCostRefreshLock.WaitAsync(stoppingToken);
        try
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var craftCostUpdater = scope.ServiceProvider.GetRequiredService<ItemCraftCostUpdater>();
                    await craftCostUpdater.UpdateCraftCosts(stoppingToken);
                },
                stoppingToken
            );
        }
        finally
        {
            _craftCostRefreshLock.Release();
        }
    }

    private async Task RunPriceSnapshotUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        SyncTrigger trigger = _triggers.Get(WorkerSyncs.Prices.Key);
        do
        {
            timer.Period = _intervalGate.GetInterval(WorkerSyncs.Prices.Key);
            // Gated by the Price-history feature (off by default on the desktop, since the snapshots grow the DB).
            if (!_featureGate.IsEnabled(WorkerFeatures.PricesSync.Key))
            {
                continue;
            }

            await RunUpdateSafely(
                async scope =>
                {
                    var priceSnapshotUpdater = scope.ServiceProvider.GetRequiredService<PriceSnapshotUpdater>();
                    await priceSnapshotUpdater.UpdatePrices(stoppingToken);
                },
                stoppingToken
            );

            // Fresh prices just landed — recompute craft costs now (they're priced from these snapshots) rather
            // than waiting for the craft-cost timer. RefreshCraftCostsSafely is itself gated on PricesSync.
            await RefreshCraftCostsSafely(stoppingToken);
        } while (await trigger.WaitForNextRunAsync(timer, stoppingToken));
    }

    private async Task RunPriceHistoryRetentionUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var retentionUpdater = scope.ServiceProvider.GetRequiredService<PriceHistoryRetentionUpdater>();
                    await retentionUpdater.DownsampleOldHistory(stoppingToken);
                },
                stoppingToken
            );
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAccountSyncUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        SyncTrigger trigger = _triggers.Get(WorkerSyncs.Account.Key);
        do
        {
            timer.Period = _intervalGate.GetInterval(WorkerSyncs.Account.Key);
            await RunUpdateSafely(
                async scope =>
                {
                    var accountSyncUpdater = scope.ServiceProvider.GetRequiredService<AccountSyncUpdater>();
                    await accountSyncUpdater.SyncAccount(stoppingToken);
                },
                stoppingToken
            );
        } while (await trigger.WaitForNextRunAsync(timer, stoppingToken));
    }

    private async Task RunUpdateSafely(Func<IServiceScope, Task> updateAction, CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            await updateAction(scope);
        }
        catch (Exception) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Update operation was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the update.");
        }
    }
}
