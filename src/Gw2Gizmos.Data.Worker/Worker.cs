using Gw2Gizmos.Data.Worker.Updaters;

namespace Gw2Gizmos.Data.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var commerceTimer = new PeriodicTimer(TimeSpan.FromHours(1));
        using var currenciesTimer = new PeriodicTimer(TimeSpan.FromDays(1));
        using var itemsTimer = new PeriodicTimer(TimeSpan.FromDays(1));
        using var recipesTimer = new PeriodicTimer(TimeSpan.FromDays(1));
        // Prices move with the trading post, so recompute the market snapshot on the commerce cadence.
        using var marketTimer = new PeriodicTimer(TimeSpan.FromHours(1));
        // Poll prices frequently to capture trading volume at fine resolution.
        using var priceSnapshotTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        // Coarsen the accumulating price history hourly so the 5-minute tier stays bounded.
        using var priceHistoryRetentionTimer = new PeriodicTimer(TimeSpan.FromHours(1));

        // Run all tasks concurrently
        await Task.WhenAll(
            RunCommerceUpdater(commerceTimer, stoppingToken),
            RunCurrenciesUpdater(currenciesTimer, stoppingToken),
            RunItemsUpdater(itemsTimer, stoppingToken),
            RunRecipesUpdater(recipesTimer, stoppingToken),
            RunMarketUpdater(marketTimer, stoppingToken),
            RunPriceSnapshotUpdater(priceSnapshotTimer, stoppingToken),
            RunPriceHistoryRetentionUpdater(priceHistoryRetentionTimer, stoppingToken)
        );
    }

    private async Task RunCommerceUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var commerceUpdater = scope.ServiceProvider.GetRequiredService<CommerceUpdater>();
                    await commerceUpdater.UpdateCommerceListings(stoppingToken);
                },
                stoppingToken
            );
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCurrenciesUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var currenciesUpdater = scope.ServiceProvider.GetRequiredService<CurrenciesUpdater>();
                    await currenciesUpdater.UpdateCurrencies(stoppingToken);
                },
                stoppingToken
            );
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunItemsUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var itemsUpdater = scope.ServiceProvider.GetRequiredService<ItemsUpdater>();
                    await itemsUpdater.UpdateItems(stoppingToken);
                },
                stoppingToken
            );
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunRecipesUpdater(PeriodicTimer recipesTimer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var recipesUpdater = scope.ServiceProvider.GetRequiredService<RecipesUpdater>();
                    await recipesUpdater.UpdateRecipes(stoppingToken);
                },
                stoppingToken
            );
        } while (await recipesTimer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunMarketUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var marketUpdater = scope.ServiceProvider.GetRequiredService<MarketUpdater>();
                    await marketUpdater.UpdateMarket(stoppingToken);
                },
                stoppingToken
            );
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunPriceSnapshotUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var priceSnapshotUpdater = scope.ServiceProvider.GetRequiredService<PriceSnapshotUpdater>();
                    await priceSnapshotUpdater.UpdatePrices(stoppingToken);
                },
                stoppingToken
            );
        } while (await timer.WaitForNextTickAsync(stoppingToken));
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
