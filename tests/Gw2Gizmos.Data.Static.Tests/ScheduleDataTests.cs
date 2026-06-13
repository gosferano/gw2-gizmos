using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>Guards on the hardcoded schedules and the <see cref="Schedule"/> helpers.</summary>
public class ScheduleDataTests
{
    public static IEnumerable<object[]> AllEvents =>
        WorldBosses.All
            .Concat(MetaEvents.All)
            .Concat(PublicInstances.All)
            .Concat(Invasions.All)
            .Select(e => new object[] { e });

    [Fact]
    public void Every_GeneratesEvenlySpacedTimesFromTheOffset()
    {
        IReadOnlyList<TimeSpan> times = Schedule.Every(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30));

        Assert.Equal(12, times.Count);
        Assert.Equal(TimeSpan.Parse("00:30"), times[0]);
        Assert.Equal(TimeSpan.Parse("02:30"), times[1]);
        Assert.Equal(TimeSpan.Parse("22:30"), times[^1]);
    }

    [Fact]
    public void At_ParsesEachTimeOfDay()
    {
        IReadOnlyList<TimeSpan> times = Schedule.At("00:00", "11:30", "19:00");

        Assert.Equal(new[] { TimeSpan.Zero, TimeSpan.Parse("11:30"), TimeSpan.Parse("19:00") }, times);
    }

    [Fact]
    public void EventIds_AreUniqueAcrossEveryCategory()
    {
        string[] ids = WorldBosses.All
            .Concat(MetaEvents.All)
            .Concat(PublicInstances.All)
            .Concat(Invasions.All)
            .Select(e => e.Id)
            .ToArray();

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(AllEvents))]
    public void EveryEvent_IsWellFormed(ScheduledEvent scheduledEvent)
    {
        Assert.False(string.IsNullOrWhiteSpace(scheduledEvent.Id));
        Assert.False(string.IsNullOrWhiteSpace(scheduledEvent.Name));
        Assert.False(string.IsNullOrWhiteSpace(scheduledEvent.Map));
        Assert.NotEmpty(scheduledEvent.DailyTimesUtc);
        Assert.True(scheduledEvent.Duration > TimeSpan.Zero);
        Assert.True(Enum.IsDefined(scheduledEvent.Expansion));

        foreach (TimeSpan time in scheduledEvent.DailyTimesUtc)
        {
            Assert.InRange(time, TimeSpan.Zero, TimeSpan.FromHours(24) - TimeSpan.FromTicks(1));
        }
    }

    [Fact]
    public void Tequatl_HasItsKnownSixDailyTimes()
    {
        // The hardcore world-boss times are the anchor the rest of the schedule was validated against.
        ScheduledEvent tequatl = WorldBosses.All.Single(e => e.Id == "tequatl");

        Assert.Equal(
            new[] { "00:00", "03:00", "07:00", "11:30", "16:00", "19:00" }.Select(TimeSpan.Parse),
            tequatl.DailyTimesUtc.OrderBy(t => t));
    }
}
