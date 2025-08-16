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
        using var itemsTimer = new PeriodicTimer(TimeSpan.FromDays(1));

        // Run both tasks concurrently
        await Task.WhenAll(
            RunCommerceUpdater(commerceTimer, stoppingToken),
            RunItemsUpdater(itemsTimer, stoppingToken)
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

    private async Task RunUpdateSafely(Func<IServiceScope, Task> updateAction, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
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
