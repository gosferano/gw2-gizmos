namespace Gw2Gizmos.Data.Static.Events;

/// <summary>Helpers for declaring an event's daily UTC spawn times concisely in the hardcoded data.</summary>
public static class Schedule
{
    /// <summary>Explicit times of day, e.g. <c>At("00:00", "11:30", "19:00")</c>.</summary>
    public static IReadOnlyList<TimeSpan> At(params string[] times) =>
        times.Select(TimeSpan.Parse).ToArray();

    /// <summary>
    /// Evenly spaced times across a day from an offset, e.g. <c>Every(2h, 0:30)</c> →
    /// 00:30, 02:30, 04:30 … 22:30. The interval must divide evenly into 24h for a clean daily loop.
    /// </summary>
    public static IReadOnlyList<TimeSpan> Every(TimeSpan interval, TimeSpan offset = default)
    {
        var times = new List<TimeSpan>();
        for (TimeSpan time = offset; time < TimeSpan.FromHours(24); time += interval)
        {
            times.Add(time);
        }

        return times;
    }
}
