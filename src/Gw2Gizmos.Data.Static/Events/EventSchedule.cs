namespace Gw2Gizmos.Data.Static.Events;

/// <summary>Every scheduled event: world bosses, metas, public instances, invasions and festival events.</summary>
public static class EventSchedule
{
    public static readonly IReadOnlyList<ScheduledEvent> All = WorldBosses.All
        .Concat(MetaEvents.All)
        .Concat(PublicInstances.All)
        .Concat(Invasions.All)
        .Concat(Festivals.All)
        .ToList();
}
