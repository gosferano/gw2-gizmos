namespace Gw2Gizmos.Gw2Api.Contract.V2.Raids;

public class Raid
{
    public string Id { get; set; }
    public RaidWing[] Wings { get; set; } = Array.Empty<RaidWing>();
}
