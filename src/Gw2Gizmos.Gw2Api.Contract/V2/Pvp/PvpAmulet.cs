namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpAmulet
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public ItemAttributes Attributes { get; set; } = null!;
}
