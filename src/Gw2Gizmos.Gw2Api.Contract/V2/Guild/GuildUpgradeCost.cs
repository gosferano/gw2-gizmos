namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildUpgradeCost
{
    public GuildUpgradeCostType Type { get; set; }
    public string? Name { get; set; }
    public int Count { get; set; }
    public int? ItemId { get; set; }
}
