namespace Gw2Gizmos.Data.Static.Events;

/// <summary>Which kind of timed event this is — drives grouping/filtering in the UI.</summary>
public enum EventKind
{
    WorldBoss,
    MetaEvent,
    PublicInstance,
    Invasion
}

/// <summary>The release an event belongs to (expansion or Living World season), for filtering the UI.</summary>
public enum Expansion
{
    CoreTyria,
    LivingWorldSeason2,
    HeartOfThorns,
    PathOfFire,
    LivingWorldSeason4,
    IcebroodSaga,
    EndOfDragons,
    SecretsOfTheObscure,
    JanthirWilds,
    VisionsOfEternity
}

/// <summary>
/// A recurring Guild Wars 2 timed event (a world boss or a map meta) with a fixed daily UTC schedule. GW2's
/// API has no spawn-timer endpoint, so the schedule is hardcoded here as times-of-day in UTC; the next
/// occurrence is computed against the clock at runtime.
/// </summary>
public sealed record ScheduledEvent
{
    /// <summary>Stable key (e.g. "tequatl") used for notification subscriptions and persistence.</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>The map the event happens on (e.g. "Sparkfly Fen").</summary>
    public required string Map { get; init; }

    public required EventKind Kind { get; init; }

    /// <summary>The release this event belongs to, for the expansion/Living-World filter.</summary>
    public required Expansion Expansion { get; init; }

    /// <summary>GW2 chat link to the event's waypoint/POI (e.g. "[&amp;BNABAAA=]"), copyable in the UI.</summary>
    public string? ChatLink { get; init; }

    /// <summary>How long the event stays active once it begins (used to show "active now").</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>The times of day, in UTC, at which the event begins each day.</summary>
    public required IReadOnlyList<TimeSpan> DailyTimesUtc { get; init; }

    /// <summary>The soonest start strictly after <paramref name="nowUtc"/>, wrapping into tomorrow if needed.</summary>
    public DateTimeOffset NextStartUtc(DateTimeOffset nowUtc)
    {
        var midnight = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);
        DateTimeOffset best = DateTimeOffset.MaxValue;

        foreach (TimeSpan time in DailyTimesUtc)
        {
            DateTimeOffset today = midnight + time;
            DateTimeOffset candidate = today > nowUtc ? today : today.AddDays(1);
            if (candidate < best)
            {
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>When the current occurrence ends if the event is active right now; otherwise null.</summary>
    public DateTimeOffset? ActiveUntilUtc(DateTimeOffset nowUtc)
    {
        var midnight = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);

        foreach (TimeSpan time in DailyTimesUtc)
        {
            // Check both today's and yesterday's start (an event begun late yesterday may still be active).
            foreach (DateTimeOffset start in new[] { midnight + time, midnight + time - TimeSpan.FromDays(1) })
            {
                if (start <= nowUtc && nowUtc < start + Duration)
                {
                    return start + Duration;
                }
            }
        }

        return null;
    }
}
