using Gw2Gizmos.Desktop.Controls;
using ScottPlot;

namespace Gw2Gizmos.Desktop.Tests;

/// <summary>
/// Date ticks for charts plotted in UTC: chosen at local midnights and hours, in order through daylight saving
/// changes.
/// </summary>
public sealed class LocalTimeTicksTests
{
    // UTC+2 in winter, UTC+3 in summer; the clocks went back 04:00 → 03:00 on 25 October 2026 and forward
    // 03:00 → 04:00 on 29 March 2026.
    private static readonly TimeZoneInfo Vilnius = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vilnius");

    private static Tick[] Ticks(DateTime fromUtc, DateTime toUtc, float pixels = 800)
    {
        var ticks = new LocalTimeTicks(Vilnius);
        using Paint paint = Paint.NewDisposablePaint();
        ticks.Regenerate(new CoordinateRange(fromUtc.ToOADate(), toUtc.ToOADate()), Edge.Bottom,
            new PixelLength(pixels), paint, new LabelStyle());
        return ticks.Ticks;
    }

    private static DateTime Utc(int month, int day, int hour) => new(2026, month, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_date_axis_takes_it()
    {
        var plot = new Plot();
        plot.Axes.DateTimeTicksBottom().TickGenerator = new LocalTimeTicks(Vilnius);

        Assert.IsType<LocalTimeTicks>(plot.Axes.Bottom.TickGenerator);
    }

    [Fact]
    public void Midnight_ticks_fall_on_local_midnight()
    {
        Tick[] major = Ticks(Utc(10, 20, 0), Utc(10, 28, 0)).Where(t => t.IsMajor).ToArray();

        // Local midnight is 21:00 UTC in summer time and 22:00 UTC after the clocks go back.
        Tick summer = Assert.Single(major, t => t.Label == "10-25");
        Assert.Equal(Utc(10, 24, 21).ToOADate(), summer.Position, 1e-9);
        Tick winter = Assert.Single(major, t => t.Label == "10-26");
        Assert.Equal(Utc(10, 25, 22).ToOADate(), winter.Position, 1e-9);
    }

    [Theory]
    [InlineData(10, 25)] // the clocks go back: 03:00–04:00 happens twice
    [InlineData(3, 29)] // the clocks go forward: 03:00–04:00 is skipped
    public void Hourly_ticks_stay_in_order_through_a_clock_change(int month, int day)
    {
        Tick[] ticks = Ticks(Utc(month, day, 0) - TimeSpan.FromHours(1), Utc(month, day, 3), pixels: 2000);

        Assert.True(ticks.Length > 3);
        Assert.Equal(ticks.Select(t => t.Position).Order(), ticks.Select(t => t.Position));
        Assert.Equal(ticks.Length, ticks.Select(t => t.Position).Distinct().Count());
    }
}
