using System.Threading.Channels;

namespace Gw2Gizmos.Data.Worker.Updaters;

public class ItemsAddedUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<ItemAddedDto> _itemsAddedChannel;
    private readonly ILogger<ItemsAddedUpdater> _logger;

    public ItemsAddedUpdater(
        IServiceProvider serviceProvider,
        Channel<ItemAddedDto> itemsAddedChannel,
        ILogger<ItemsAddedUpdater> logger
    )
    {
        _serviceProvider = serviceProvider;
        _itemsAddedChannel = itemsAddedChannel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Item processing worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await _itemsAddedChannel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (_itemsAddedChannel.Reader.TryRead(out ItemAddedDto? itemAdded))
                    {
                        using IServiceScope scope = _serviceProvider.CreateScope();
                        var commerceUpdater = scope.ServiceProvider.GetRequiredService<CommerceUpdater>();
                        try
                        {
                            // Process the item
                            _logger.LogInformation("Processing item with ID: {ItemId}", itemAdded.ItemId);
                            await commerceUpdater.UpdateCommerceListingsForItems([itemAdded.ItemId], stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing item with ID: {ItemId}", itemAdded.ItemId);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in item processing worker.");
            }
        }

        _logger.LogInformation("Item processing worker stopped.");
    }
}
