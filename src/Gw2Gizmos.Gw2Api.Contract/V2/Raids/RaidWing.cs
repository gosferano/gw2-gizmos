namespace Gw2Gizmos.Gw2Api.Contract.V2.Raids;

public class RaidWing
{
    public string Id { get; set; }
    public RaidWingEvent[] Events { get; set; } = Array.Empty<RaidWingEvent>();
}
