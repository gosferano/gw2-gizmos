using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Data.Static.Tests;

public class ScheduledEventTests
{
    private static ScheduledEvent Event(TimeSpan duration, params string[] times) =>
        new()
        {
            Id = "test",
            Name = "Test",
            Map = "Test Map",
            Kind = EventKind.WorldBoss,
            Expansion = Expansion.CoreTyria,
            Duration = duration,
            DailyTimesUtc = times.Select(TimeSpan.Parse).ToList(),
        };

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void NextStartUtc_ReturnsLaterToday_WhenStartIsStillAhead()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "12:00");

        DateTimeOffset next = e.NextStartUtc(Utc(2026, 6, 15, 10, 0));

        Assert.Equal(Utc(2026, 6, 15, 12, 0), next);
    }

    [Fact]
    public void NextStartUtc_WrapsToTomorrow_WhenTodaysStartHasPassed()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "12:00");

        DateTimeOffset next = e.NextStartUtc(Utc(2026, 6, 15, 13, 0));

        Assert.Equal(Utc(2026, 6, 16, 12, 0), next);
    }

    [Fact]
    public void NextStartUtc_IsStrictlyAfterNow_SoAnExactStartReturnsTomorrow()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "12:00");

        DateTimeOffset next = e.NextStartUtc(Utc(2026, 6, 15, 12, 0));

        Assert.Equal(Utc(2026, 6, 16, 12, 0), next);
    }

    [Fact]
    public void NextStartUtc_PicksTheSoonestOfManyTimes()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "06:00", "18:00");

        Assert.Equal(Utc(2026, 6, 15, 18, 0), e.NextStartUtc(Utc(2026, 6, 15, 10, 0)));
        Assert.Equal(Utc(2026, 6, 16, 6, 0), e.NextStartUtc(Utc(2026, 6, 15, 20, 0)));
    }

    [Fact]
    public void ActiveUntilUtc_ReturnsEnd_WhenInsideTheWindow()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "12:00");

        DateTimeOffset? end = e.ActiveUntilUtc(Utc(2026, 6, 15, 12, 5));

        Assert.Equal(Utc(2026, 6, 15, 12, 15), end);
    }

    [Fact]
    public void ActiveUntilUtc_IsNull_AfterTheWindowEnds()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "12:00");

        Assert.Null(e.ActiveUntilUtc(Utc(2026, 6, 15, 12, 20)));
    }

    [Fact]
    public void ActiveUntilUtc_IsNull_BeforeTheStart()
    {
        ScheduledEvent e = Event(TimeSpan.FromMinutes(15), "12:00");

        Assert.Null(e.ActiveUntilUtc(Utc(2026, 6, 15, 11, 0)));
    }

    [Fact]
    public void ActiveUntilUtc_HandlesAWindowThatStartedYesterdayAndSpansMidnight()
    {
        // Begins 23:50, runs 30 min → ends 00:20 the next day.
        ScheduledEvent e = Event(TimeSpan.FromMinutes(30), "23:50");

        DateTimeOffset? end = e.ActiveUntilUtc(Utc(2026, 6, 16, 0, 10));

        Assert.Equal(Utc(2026, 6, 16, 0, 20), end);
    }
}
