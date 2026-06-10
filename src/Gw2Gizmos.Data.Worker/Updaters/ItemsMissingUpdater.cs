using System.Threading.Channels;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Background consumer of the <see cref="ItemMissingDto"/> channel. Batches item ids that appear
/// on the market but are absent from the items table and triggers <see cref="ItemsUpdater"/> to
/// fetch their full item data.
/// </summary>
public class ItemsMissingUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<ItemMissingDto> _itemsMissingChannel;
    private readonly ILogger<ItemsMissingUpdater> _logger;

    private const int BatchSize = 50;

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
                    var itemIds = new List<int>();

                    while (_itemsMissingChannel.Reader.TryRead(out ItemMissingDto? itemMissing))
                    {
                        itemIds.Add(itemMissing.ItemId);

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
                _logger.LogError(ex, "Unexpected error in items missing updater.");
            }
        }

        _logger.LogInformation("Items missing updater stopped.");
    }

    private async Task ProcessBatchAsync(int[] itemIds, CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var itemsUpdater = scope.ServiceProvider.GetRequiredService<ItemsUpdater>();
            _logger.LogInformation("Processing missing items with IDs: {ItemIds}", string.Join(", ", itemIds));
            await itemsUpdater.UpdateItemsWithIds(itemIds, stoppingToken);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing missing items with IDs: {ItemIds}", string.Join(", ", itemIds));
        }
    }
}
