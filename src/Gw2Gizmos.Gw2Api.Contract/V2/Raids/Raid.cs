namespace Gw2Gizmos.Gw2Api.Contract.V2.Raids;

public sealed class Raid
{
    public string Id { get; set; } = null!;
    public RaidWing[] Wings { get; set; } = Array.Empty<RaidWing>();
}
