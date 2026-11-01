using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Coarsening the price history: 5-minute points older than an hour collapse to one per hour, points older than two
/// weeks to one per day, each keeping its bucket's latest prices and its total traded volume.
/// </summary>
public sealed class PriceHistoryRetentionTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private async Task DownsampleAsync()
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        var provider = new ActiveDbProvider([new SqliteDbProvider()], new ConfigurationBuilder().Build());
        await new PriceHistoryRetentionUpdater(db, provider, NullLogger<PriceHistoryRetentionUpdater>.Instance)
            .DownsampleOldHistory(CancellationToken.None);
    }

    private void Seed(params PriceSnapshot[] snapshots)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        db.PriceSnapshots.AddRange(snapshots);
        db.SaveChanges();
    }

    private static PriceSnapshot Point(int itemId, DateTimeOffset at, int buy, int sold, int bought = 0) =>
        new() { ItemId = itemId, TimestampUtc = at, Buy = buy, Sell = buy + 1, Sold = sold, Bought = bought };

    private List<PriceSnapshot> Points() =>
        _fixture.NewContext().PriceSnapshots.AsNoTracking().OrderBy(p => p.Id).ToList();

    // The start of the hour three hours ago: its points are older than an hour, and all in one bucket.
    private static DateTimeOffset HourAgo3()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).AddHours(-3);
    }

    [Fact]
    public async Task Points_older_than_an_hour_collapse_to_the_latest_of_each_hour_with_its_volume()
    {
        DateTimeOffset hour = HourAgo3();
        DateTimeOffset recent = DateTimeOffset.UtcNow.AddMinutes(-10);
        Seed(
            Point(1, hour.AddMinutes(5), buy: 100, sold: 2, bought: 1),
            Point(1, hour.AddMinutes(10), buy: 110, sold: 3, bought: 4),
            Point(1, hour.AddMinutes(15), buy: 120, sold: 5, bought: 0),
            Point(2, hour.AddMinutes(5), buy: 50, sold: 7), // alone in its hour: left as is
            Point(1, recent, buy: 130, sold: 1),
            Point(1, recent.AddMinutes(5), buy: 140, sold: 1));

        await DownsampleAsync();

        List<PriceSnapshot> points = Points();
        PriceSnapshot kept = Assert.Single(points, p => p.ItemId == 1 && p.TimestampUtc < recent);
        Assert.Equal((120, 10, 5), (kept.Buy, kept.Sold, kept.Bought));
        Assert.Equal(7, Assert.Single(points, p => p.ItemId == 2).Sold);
        Assert.Equal(2, points.Count(p => p.TimestampUtc >= recent)); // the last hour keeps full resolution
    }

    [Fact]
    public async Task Points_older_than_two_weeks_collapse_to_one_a_day()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset day = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).AddDays(-20);
        Seed(
            Point(1, day.AddHours(1), buy: 100, sold: 2),
            Point(1, day.AddHours(9), buy: 110, sold: 3),
            Point(1, day.AddHours(23), buy: 120, sold: 4));

        await DownsampleAsync();

        PriceSnapshot kept = Assert.Single(Points());
        Assert.Equal((120, 9), (kept.Buy, kept.Sold));
    }

    [Fact]
    public async Task A_second_run_changes_nothing()
    {
        DateTimeOffset hour = HourAgo3();
        Seed(Point(1, hour.AddMinutes(5), buy: 100, sold: 2), Point(1, hour.AddMinutes(10), buy: 110, sold: 3));

        await DownsampleAsync();
        await DownsampleAsync();

        Assert.Equal(5, Assert.Single(Points()).Sold);
    }
}
