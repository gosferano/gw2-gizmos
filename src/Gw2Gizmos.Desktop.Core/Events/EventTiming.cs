using System;
using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>Where a scheduled event stands at a moment: active, or how long until it starts.</summary>
/// <param name="SortKey">When it ends if active, otherwise when it next starts: the sooner, the more pressing.</param>
/// <param name="Countdown">E.g. "in 12:34", "in 1h 05m" or "active · 8:12 left".</param>
/// <param name="ShortCountdown">E.g. "12:34", "1h 05m" or "now", for tight spots.</param>
public sealed record EventTiming(bool IsActive, DateTimeOffset SortKey, string Countdown, string ShortCountdown)
{
    /// <summary>
    /// How close a start counts as soon: such an event's timer is highlighted (yellow; active ones are green).
    /// </summary>
    public static readonly TimeSpan SoonWindow = TimeSpan.FromMinutes(15);

    /// <summary>Not active, and starting within <see cref="SoonWindow"/>.</summary>
    public bool IsSoon { get; init; }

    public static EventTiming Of(ScheduledEvent scheduled, DateTimeOffset now)
    {
        if (scheduled.ActiveUntilUtc(now) is { } end)
        {
            return new EventTiming(true, end, $"active · {Format(end - now)} left", "now");
        }

        DateTimeOffset next = scheduled.NextStartUtc(now);
        string untilNext = Format(next - now);
        return new EventTiming(false, next, $"in {untilNext}", untilNext) { IsSoon = next - now <= SoonWindow };
    }

    // Hours+minutes when far out; minutes:seconds (ticking) when under an hour.
    private static string Format(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes:00}m"
            : $"{span.Minutes}:{span.Seconds:00}";
    }
}
