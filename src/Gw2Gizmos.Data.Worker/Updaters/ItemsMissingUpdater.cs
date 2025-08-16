using System.Threading.Channels;

namespace Gw2Gizmos.Data.Worker.Updaters;

public class ItemsMissingUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<ItemMissingDto> _itemsMissingChannel;
    private readonly ILogger<ItemsMissingUpdater> _logger;

    public ItemsMissingUpdater(
        IServiceProvider serviceProvider,
        Channel<ItemMissingDto> itemsMissingChannel,
        ILogger<ItemsMissingUpdater> logger
    )
    {
        _serviceProvider = serviceProvider;
        _itemsMissingChannel = itemsMissingChannel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Items missing updater started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await _itemsMissingChannel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (_itemsMissingChannel.Reader.TryRead(out ItemMissingDto? itemMissing))
                    {
                        using IServiceScope scope = _serviceProvider.CreateScope();
                        var itemsUpdater = scope.ServiceProvider.GetRequiredService<ItemsUpdater>();

                        try
                        {
                            _logger.LogInformation("Processing missing item with ID: {ItemId}", itemMissing.ItemId);
                            await itemsUpdater.UpdateItemsWithIds([itemMissing.ItemId], stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing missing item with ID: {ItemId}", itemMissing.ItemId);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break; // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in items missing updater.");
            }
        }

        _logger.LogInformation("Items missing updater stopped.");
    }
}
