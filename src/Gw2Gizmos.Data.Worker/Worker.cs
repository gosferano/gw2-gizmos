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
        // Profit moves with the trading post, so recompute on the commerce cadence.
        using var profitableRecipesTimer = new PeriodicTimer(TimeSpan.FromHours(1));

        // Run all tasks concurrently
        await Task.WhenAll(
            RunCommerceUpdater(commerceTimer, stoppingToken),
            RunCurrenciesUpdater(currenciesTimer, stoppingToken),
            RunItemsUpdater(itemsTimer, stoppingToken),
            RunRecipesUpdater(recipesTimer, stoppingToken),
            RunProfitableRecipesUpdater(profitableRecipesTimer, stoppingToken)
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

    private async Task RunProfitableRecipesUpdater(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        do
        {
            await RunUpdateSafely(
                async scope =>
                {
                    var profitableRecipesUpdater =
                        scope.ServiceProvider.GetRequiredService<ProfitableRecipesUpdater>();
                    await profitableRecipesUpdater.UpdateProfitableRecipes(stoppingToken);
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
