namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildTreasuryItem
{
    public int ItemId { get; set; }
    public int Count { get; set; }
    public GuildTreasuryItemUpgrade[] NeededBy { get; set; } = Array.Empty<GuildTreasuryItemUpgrade>();
}
