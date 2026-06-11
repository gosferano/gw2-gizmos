using System.Threading.Channels;
using Gw2Gizmos.Gw2Api.Client;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Item master-data refresh trigger. <see cref="UpdateItems"/> enqueues every item id onto the
/// items channel; the single consumer (<see cref="ItemsMissingUpdater"/>) fetches and upserts them.
/// Enqueuing rather than writing directly keeps the scheduled refresh from racing the
/// commerce-driven backfill (which also enqueues ids) — one consumer means no concurrent inserts.
/// </summary>
public class ItemsUpdater
{
    private readonly ILogger<ItemsUpdater> _logger;
    private readonly ChannelWriter<ItemMissingDto> _itemsMissingWriter;
    private readonly Gw2ApiClient _apiClient;

    public ItemsUpdater(
        IGw2ApiClientFactory apiClientFactory,
        ILogger<ItemsUpdater> logger,
        Channel<ItemMissingDto> itemsMissing
    )
    {
        _logger = logger;
        _itemsMissingWriter = itemsMissing.Writer;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    /// <summary>
    /// Enqueues every item id onto the items channel for the single consumer to upsert, rather than
    /// writing directly — so the scheduled refresh never races the commerce-driven backfill.
    /// </summary>
    public async Task UpdateItems(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting items update...");

        // Get all item IDs from API
        int[]? allItemIds = await _apiClient.V2.Items.GetIds(stoppingToken);

        if (allItemIds is null || allItemIds.Length == 0)
        {
            _logger.LogWarning("Items API returned no ids; skipping items update.");
            return;
        }

        _logger.LogInformation("Queueing {Count} item ids for upsert.", allItemIds.Length);

        foreach (int itemId in allItemIds)
        {
            await _itemsMissingWriter.WriteAsync(new ItemMissingDto { ItemId = itemId }, stoppingToken);
        }

        _logger.LogInformation("Items update queued.");
    }
}