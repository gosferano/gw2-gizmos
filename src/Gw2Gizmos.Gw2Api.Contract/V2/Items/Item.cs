namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class Item
{
    public int Id { get; set; }
    public string ChatLink { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public ItemType Type { get; set; }
    public ItemRarity Rarity { get; set; }
    public int Level { get; set; }
    public int VendorValue { get; set; }
    public int? DefaultSkin { get; set; }
    public ItemFlag[] Flags { get; set; } = Array.Empty<ItemFlag>();
    public ItemGameType[] GameTypes { get; set; } = Array.Empty<ItemGameType>();
    public ItemRestriction[] Restrictions { get; set; } = Array.Empty<ItemRestriction>();
}
