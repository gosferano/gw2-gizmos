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
    private readonly ActiveDbProvider _provider;
    private readonly ILogger<PriceHistoryRetentionUpdater> _logger;

    public PriceHistoryRetentionUpdater(
        Gw2GizmosDbContext dbContext,
        ActiveDbProvider provider,
        ILogger<PriceHistoryRetentionUpdater> logger
    )
    {
        _dbContext = dbContext;
        _provider = provider;
        _logger = logger;
    }

    public async Task DownsampleOldHistory(CancellationToken stoppingToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset hourCutoff = now - FineResolutionWindow;
        DateTimeOffset dayCutoff = now - HourlyResolutionWindow;

        // Collapse the 5-minute tier (older than an hour, within the last two weeks) to one point per hour.
        // Time-bucket SQL is provider-specific, so it comes from the active provider's dialect.
        int collapsedToHourly = await CollapseBucketAsync(
            _provider.Provider.Dialect.HourBucket,
            dayCutoff,
            hourCutoff,
            stoppingToken
        );

        // Collapse everything older than two weeks to one point per day.
        int collapsedToDaily = await CollapseBucketAsync(
            _provider.Provider.Dialect.DayBucket,
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
    /// Collapses every (item, time-bucket) group in <c>[lower, upper)</c> with more than one row to its latest row,
    /// folding the group's total <c>Sold</c>/<c>Bought</c> into that survivor and deleting the rest. Returns the rows
    /// deleted. First, without taking the write lock, it works out which groups still need collapsing, their totals
    /// and the rows to delete, into temp tables (they live in SQLite's temp database). Then it updates and deletes only
    /// those rows, in one transaction: the write lock is held for a moment, not the minutes a sweep over the whole
    /// window took (every other write waited out its timeout and failed), and a stop part-way can't leave a survivor
    /// holding its group's total beside rows the next run would count again.
    /// </summary>
    private async Task<int> CollapseBucketAsync(
        Func<string, string> bucketOf,
        DateTimeOffset lower,
        DateTimeOffset upper,
        CancellationToken stoppingToken
    )
    {
        // The time bounds are compared against TimestampUtc, which is stored as UTC ticks (the model's value
        // converter) — and raw SQL bypasses that converter, so bind the bounds as ticks too. Binding DateTimeOffset
        // here would compare an integer column to a text datetime and match nothing (so nothing would ever collapse).
        object[] bounds = [lower.UtcTicks, upper.UtcTicks];

        // bucketOf builds the time-bucket expression for a given table qualifier; it's fixed code (not user input) so
        // it's interpolated as literal SQL, while the time bounds are bound as parameters. EF1002 is safe to suppress
        // for the same reason.
        string bucket = bucketOf("PriceSnapshots");
        string rowBucket = bucketOf("p");

        // Temp tables belong to the connection, so it's held open across the statements.
        await _dbContext.Database.OpenConnectionAsync(stoppingToken);
        try
        {
#pragma warning disable EF1002
            await _dbContext.Database.ExecuteSqlRawAsync(
                "DROP TABLE IF EXISTS temp.Collapse; DROP TABLE IF EXISTS temp.Doomed;", stoppingToken);
            await _dbContext.Database.ExecuteSqlRawAsync(
                $@"CREATE TEMP TABLE Collapse AS
                   SELECT ItemId, {bucket} AS Bucket, MAX(Id) AS Keep, SUM(Sold) AS Sold, SUM(Bought) AS Bought
                   FROM PriceSnapshots
                   WHERE TimestampUtc >= {{0}} AND TimestampUtc < {{1}}
                   GROUP BY ItemId, Bucket
                   HAVING COUNT(*) > 1",
                bounds,
                stoppingToken
            );
            await _dbContext.Database.ExecuteSqlRawAsync(
                $@"CREATE TEMP TABLE Doomed AS
                   SELECT p.Id FROM temp.Collapse c JOIN PriceSnapshots p ON p.ItemId = c.ItemId
                   WHERE p.TimestampUtc >= {{0}} AND p.TimestampUtc < {{1}}
                     AND {rowBucket} = c.Bucket AND p.Id <> c.Keep",
                bounds,
                stoppingToken
            );
#pragma warning restore EF1002

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(stoppingToken);
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE PriceSnapshots SET Sold = c.Sold, Bought = c.Bought
                  FROM temp.Collapse c
                  WHERE PriceSnapshots.Id = c.Keep",
                stoppingToken
            );
            int deleted = await _dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM PriceSnapshots WHERE Id IN (SELECT Id FROM temp.Doomed)", stoppingToken);
            await transaction.CommitAsync(stoppingToken);
            return deleted;
        }
        finally
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "DROP TABLE IF EXISTS temp.Collapse; DROP TABLE IF EXISTS temp.Doomed;", CancellationToken.None);
            await _dbContext.Database.CloseConnectionAsync();
        }
    }
}
