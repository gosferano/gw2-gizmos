namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Item
{
     public int Id { get; set; }
     public string ChatLink { get; set; }
     public string Name { get; set; }
     public string? Icon { get; set; }
     public string? Description { get; set; }
     public ItemType Type { get; set; }
     public ItemRarity Rarity { get; set; }
     public int Level { get; set; }
     public int VendorValue { get; set; }
     public int? DefaultSkint { get; set; }
     public ItemFlag[] Flags { get; set; } = Array.Empty<ItemFlag>();
     public ItemGameType[] GameTypes { get; set; } = Array.Empty<ItemGameType>();
     public ItemRestriction[] Restrictions { get; set; } = Array.Empty<ItemRestriction>();
}