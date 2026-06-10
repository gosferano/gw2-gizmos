using System.Threading.Channels;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Background consumer of the <see cref="ItemAddedDto"/> channel. Batches newly-added item ids
/// and triggers <see cref="CommerceUpdater"/> to backfill their trading-post listings.
/// </summary>
public class ItemsAddedUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<ItemAddedDto> _itemsAddedChannel;
    private readonly ILogger<ItemsAddedUpdater> _logger;

    private const int BatchSize = 50;

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
                    var itemIds = new List<int>();

                    while (_itemsAddedChannel.Reader.TryRead(out ItemAddedDto? itemAdded))
                    {
                        itemIds.Add(itemAdded.ItemId);

                        if (itemIds.Count == BatchSize)
                        {
                            break;
                        }
                    }

                    if (itemIds.Count != 0)
                    {
                        await ProcessBatchAsync(itemIds.ToArray(), stoppingToken);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in item processing worker.");
            }
        }

        _logger.LogInformation("Item added updater stopped.");
    }

    private async Task ProcessBatchAsync(int[] itemIds, CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var commerceUpdater = scope.ServiceProvider.GetRequiredService<CommerceUpdater>();
            _logger.LogInformation("Processing items with IDs: {ItemIds}", string.Join(", ", itemIds));
            await commerceUpdater.UpdateCommerceListingsForItems(itemIds, stoppingToken);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing items with IDs: {ItemIds}", string.Join(", ", itemIds));
        }
    }
}
