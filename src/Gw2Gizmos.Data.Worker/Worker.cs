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
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        await RunUpdateSafely(stoppingToken);

        // Then run periodically
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunUpdateSafely(stoppingToken);
        }
    }

    private async Task RunUpdateSafely(CancellationToken stoppingToken)
    {
        try
        {
            await RunUpdate(stoppingToken);
        }
        catch (Exception) when (stoppingToken.IsCancellationRequested)
        {
            // If the operation was cancelled, we just exit
            _logger.LogInformation("Update operation was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the update.");
        }
    }

    private async Task RunUpdate(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var itemsUpdater = scope.ServiceProvider.GetRequiredService<ItemsUpdater>();
        await itemsUpdater.UpdateItems(stoppingToken);
        var commerceUpdater = scope.ServiceProvider.GetRequiredService<CommerceUpdater>();
        await commerceUpdater.UpdateCommerceListings(stoppingToken);
    }
}
