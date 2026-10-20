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

    /// <summary>The attuned or infused versions this item can be made into (rings, accessories, back items).</summary>
    public ItemUpgrade[] UpgradesInto { get; set; } = Array.Empty<ItemUpgrade>();

    /// <summary>The items this one is an attuned or infused version of.</summary>
    public ItemUpgrade[] UpgradesFrom { get; set; } = Array.Empty<ItemUpgrade>();
}

/// <summary>An item linked by an upgrade, e.g. a ring's attuned version.</summary>
public sealed class ItemUpgrade
{
    /// <summary>"Attunement" or "Infusion".</summary>
    public string Upgrade { get; set; } = null!;
    public int ItemId { get; set; }
}
