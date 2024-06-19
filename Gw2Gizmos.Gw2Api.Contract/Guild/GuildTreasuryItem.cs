namespace Gw2Gizmos.Gw2Api.Contract.Guild;

public class GuildTreasuryItem
{
    public int ItemId { get; set; }
    public int Count { get; set; }
    public GuildTreasuryItemUpgrade[] NeededBy { get; set; } = Array.Empty<GuildTreasuryItemUpgrade>();
}
