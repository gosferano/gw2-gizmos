namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class GuildStashSection
{
    public int UpgradeId { get; set; }
    public int Size { get; set; }
    public int Coins { get; set; }
    public string? Note { get; set; }
    public GuildStashItem?[] Inventory { get; set; } = Array.Empty<GuildStashItem?>();
}
