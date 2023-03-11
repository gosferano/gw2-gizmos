namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ItemRarity
{
    public static ItemRarity Junk = new(ItemRarities.Junk);
    public static ItemRarity Basic = new(ItemRarities.Basic);
    public static ItemRarity Fine = new(ItemRarities.Fine);
    public static ItemRarity Masterwork = new(ItemRarities.Masterwork);
    public static ItemRarity Rare = new(ItemRarities.Rare);
    public static ItemRarity Exotic = new(ItemRarities.Exotic);
    public static ItemRarity Ascended = new(ItemRarities.Ascended);
    public static ItemRarity Legendary = new(ItemRarities.Legendary);
    
    public string Value { get; }

    private ItemRarity(string value)
    {
        Value = value;
    }

    public static implicit operator ItemRarity(string value) => new(value);
}