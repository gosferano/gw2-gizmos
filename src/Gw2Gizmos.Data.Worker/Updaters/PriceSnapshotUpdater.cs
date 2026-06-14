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
/// without a database read. On the first poll after a (re)start the baseline is reseeded from the most recent
/// snapshot when it's recent (≤ <see cref="BaselineMaxAge"/>), so a quick relaunch doesn't drop the interval's
/// volume; otherwise (no history, or a long gap) that poll records zero volume rather than a spurious spike.
/// </para>
/// </summary>
public class PriceSnapshotUpdater
{
    private const int PageSize = 200;

    // How recent the last stored snapshot must be to reuse it as the volume baseline after a restart. Beyond
    // this (the app was closed a while) the supply/demand delta would span too long to be a meaningful
    // per-interval volume, so we drop it instead.
    private static readonly TimeSpan BaselineMaxAge = TimeSpan.FromMinutes(5);

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

        // First poll after a (re)start: reseed the volume baseline from the latest snapshot if it's recent, so a
        // quick relaunch keeps computing volume rather than dropping the interval that spans the restart.
        if (_previous.Count == 0)
        {
            _previous = LoadRecentBaseline();
        }

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
                // The API returns an empty book as unit_price 0 (not a missing object), so treat any
                // non-positive price as "no orders" and store null rather than an ambiguous 0.
                int? buy = price.Buys is { UnitPrice: > 0 } buys ? buys.UnitPrice : null;
                int? sell = price.Sells is { UnitPrice: > 0 } sells ? sells.UnitPrice : null;
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

    /// <summary>
    /// Loads the per-item supply/demand from each item's most recent snapshot, keeping only those newer than
    /// <see cref="BaselineMaxAge"/> — the volume baseline for the first poll after a restart. Empty on a fresh
    /// database or after a long downtime (so that poll records zero volume). Best-effort: a read failure just
    /// yields an empty baseline.
    /// </summary>
    private Dictionary<int, (int Supply, int Demand)> LoadRecentBaseline()
    {
        var baseline = new Dictionary<int, (int Supply, int Demand)>();
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            IQueryable<long> latestIds = dbContext.PriceSnapshots
                .AsNoTracking()
                .GroupBy(snapshot => snapshot.ItemId)
                .Select(group => group.Max(snapshot => snapshot.Id));

            // Materialize first, then filter by age in memory: SQLite can't compare DateTimeOffset in SQL.
            var latest = dbContext.PriceSnapshots
                .AsNoTracking()
                .Where(snapshot => latestIds.Contains(snapshot.Id))
                .Select(snapshot => new { snapshot.ItemId, snapshot.Supply, snapshot.Demand, snapshot.TimestampUtc })
                .ToList();

            DateTimeOffset cutoff = DateTimeOffset.UtcNow - BaselineMaxAge;
            foreach (var snapshot in latest)
            {
                if (snapshot.TimestampUtc >= cutoff)
                {
                    baseline[snapshot.ItemId] = (snapshot.Supply, snapshot.Demand);
                }
            }

            if (baseline.Count > 0)
            {
                _logger.LogInformation(
                    "Reusing volume baseline from {Count} recent snapshot(s) (≤ {Minutes} min old).",
                    baseline.Count,
                    BaselineMaxAge.TotalMinutes
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reseed the volume baseline; the first poll will record zero volume.");
        }

        return baseline;
    }
}
