namespace Gw2Gizmos.Gw2Api.Contract.V2.Raids;

public sealed class RaidWingEvent
{
    public string Id { get; set; } = null!;
    public RaidWingEventType Type { get; set; }
}
