using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Keeps the append-only price history bounded by progressively coarsening it: the most recent points stay
/// at full 5-minute resolution, points older than an hour collapse to one per hour, and points older than
/// two weeks collapse to one per day. When points are collapsed, prices and demand/supply take the latest
/// value of the bucket while <c>Sold</c>/<c>Bought</c> are <em>summed</em>, so total traded volume over any
/// range is preserved exactly regardless of how coarse the surviving resolution is.
/// </summary>
public class PriceHistoryRetentionUpdater
{
    /// <summary>Points younger than this keep full 5-minute resolution; older ones collapse to hourly.</summary>
    private static readonly TimeSpan FineResolutionWindow = TimeSpan.FromHours(1);

    /// <summary>Points younger than this keep hourly resolution; older ones collapse to daily.</summary>
    private static readonly TimeSpan HourlyResolutionWindow = TimeSpan.FromDays(14);

    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<PriceHistoryRetentionUpdater> _logger;

    public PriceHistoryRetentionUpdater(Gw2GizmosDbContext dbContext, ILogger<PriceHistoryRetentionUpdater> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task DownsampleOldHistory(CancellationToken stoppingToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset hourCutoff = now - FineResolutionWindow;
        DateTimeOffset dayCutoff = now - HourlyResolutionWindow;

        // Collapse the 5-minute tier (older than an hour, within the last two weeks) to one point per hour.
        int collapsedToHourly = await CollapseBucketAsync(
            qualifier => $"strftime('%Y-%m-%d %H', {qualifier}.TimestampUtc)",
            dayCutoff,
            hourCutoff,
            stoppingToken
        );

        // Collapse everything older than two weeks to one point per day.
        int collapsedToDaily = await CollapseBucketAsync(
            qualifier => $"date({qualifier}.TimestampUtc)",
            DateTimeOffset.MinValue,
            dayCutoff,
            stoppingToken
        );

        _logger.LogInformation(
            "Price-history retention: collapsed {Hourly} points to hourly and {Daily} to daily.",
            collapsedToHourly,
            collapsedToDaily
        );
    }

    /// <summary>
    /// Collapses every (item, time-bucket) group in <c>[lower, upper)</c> to its latest row, folding the
    /// group's total <c>Sold</c>/<c>Bought</c> into that survivor and deleting the rest. Idempotent: a
    /// bucket already reduced to one row sums to itself and deletes nothing. Returns the rows deleted.
    /// </summary>
    private async Task<int> CollapseBucketAsync(
        Func<string, string> bucketOf,
        DateTimeOffset lower,
        DateTimeOffset upper,
        CancellationToken stoppingToken
    )
    {
        // bucketOf builds the time-bucket expression for a given table qualifier; it's fixed code (not
        // user input) so it's interpolated as literal SQL, while the time bounds are bound as parameters.
        // The inner subquery aliases the table 'p', so the outer row must be qualified explicitly —
        // otherwise an unqualified column would bind to 'p' and the bucket match would always be true.
        string inner = bucketOf("p");
        string outer = bucketOf("PriceSnapshots");
        string survivors =
            $"SELECT MAX(Id) FROM PriceSnapshots WHERE TimestampUtc >= {{0}} AND TimestampUtc < {{1}} "
            + $"GROUP BY ItemId, {outer}";

        // Fold each (item, bucket) group's total volume into its latest row before deleting the rest.
        await _dbContext.Database.ExecuteSqlRawAsync(
            $@"UPDATE PriceSnapshots
               SET Sold = (SELECT SUM(p.Sold) FROM PriceSnapshots p
                           WHERE p.ItemId = PriceSnapshots.ItemId AND {inner} = {outer}
                             AND p.TimestampUtc >= {{0}} AND p.TimestampUtc < {{1}}),
                   Bought = (SELECT SUM(p.Bought) FROM PriceSnapshots p
                             WHERE p.ItemId = PriceSnapshots.ItemId AND {inner} = {outer}
                               AND p.TimestampUtc >= {{0}} AND p.TimestampUtc < {{1}})
               WHERE TimestampUtc >= {{0}} AND TimestampUtc < {{1}}
                 AND Id IN ({survivors})",
            new object[] { lower, upper },
            stoppingToken
        );

        return await _dbContext.Database.ExecuteSqlRawAsync(
            $@"DELETE FROM PriceSnapshots
               WHERE TimestampUtc >= {{0}} AND TimestampUtc < {{1}}
                 AND Id NOT IN ({survivors})",
            new object[] { lower, upper },
            stoppingToken
        );
    }
}
