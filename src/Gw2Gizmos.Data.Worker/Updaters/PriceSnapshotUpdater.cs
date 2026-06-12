using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Polls <c>/v2/commerce/prices</c> for every tradeable item and appends a price-history point per item,
/// including the units traded since the previous poll (Sold = drop in supply, Bought = drop in demand —
/// the standard GW2 volume estimate). Runs every few minutes so volume is captured at fine resolution; the
/// retention pass downsamples older points later.
/// <para>
/// Registered as a singleton so it can hold the previous poll's totals in memory and compute the deltas
/// without a database read. The map is empty on the first poll and after a restart, so that poll records
/// zero volume rather than a spurious spike.
/// </para>
/// </summary>
public class PriceSnapshotUpdater
{
    private const int PageSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<PriceSnapshotUpdater> _logger;

    private Dictionary<int, (int Supply, int Demand)> _previous = new();

    public PriceSnapshotUpdater(
        IServiceScopeFactory scopeFactory,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<PriceSnapshotUpdater> logger
    )
    {
        _scopeFactory = scopeFactory;
        _apiClient = apiClientFactory.Create(Locale.English);
        _logger = logger;
    }

    public async Task UpdatePrices(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting price snapshot poll...");

        int[]? itemIds = await _apiClient.V2.Commerce.Prices.GetIds(stoppingToken);
        if (itemIds is null || itemIds.Length == 0)
        {
            _logger.LogWarning("Commerce prices API returned no ids; skipping price snapshot.");
            return;
        }

        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        var current = new Dictionary<int, (int Supply, int Demand)>(itemIds.Length);
        var rows = new List<PriceSnapshot>(itemIds.Length);

        for (var i = 0; i < itemIds.Length; i += PageSize)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            int[] pageIds = itemIds.Skip(i).Take(PageSize).ToArray();
            CommercePrices[]? prices = await _apiClient.V2.Commerce.Prices.GetByIds(pageIds, stoppingToken);
            if (prices is null)
            {
                continue;
            }

            foreach (CommercePrices price in prices)
            {
                int buy = price.Buys?.UnitPrice ?? 0;
                int sell = price.Sells?.UnitPrice ?? 0;
                int demand = price.Buys?.Quantity ?? 0;
                int supply = price.Sells?.Quantity ?? 0;

                int sold = 0;
                int bought = 0;
                if (_previous.TryGetValue(price.Id, out (int Supply, int Demand) prev))
                {
                    sold = Math.Max(0, prev.Supply - supply);
                    bought = Math.Max(0, prev.Demand - demand);
                }

                current[price.Id] = (supply, demand);
                rows.Add(
                    new PriceSnapshot
                    {
                        ItemId = price.Id,
                        TimestampUtc = timestamp,
                        Buy = buy,
                        Sell = sell,
                        Demand = demand,
                        Supply = supply,
                        Sold = sold,
                        Bought = bought
                    }
                );
            }
        }

        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            await dbContext.PriceSnapshots.AddRangeAsync(rows, stoppingToken);
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        // Only adopt this poll as the baseline once it's safely persisted.
        _previous = current;
        _logger.LogInformation("Price snapshot poll completed: {Count} items recorded.", rows.Count);
    }
}
