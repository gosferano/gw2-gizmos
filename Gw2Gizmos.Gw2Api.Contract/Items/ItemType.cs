namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ItemType
{
    public static ItemType Armor = new(ItemTypes.Armor);
    public static ItemType Back = new(ItemTypes.Back);
    public static ItemType Bag = new(ItemTypes.Bag);
    public static ItemType Consumable = new(ItemTypes.Consumable);
    public static ItemType Container = new(ItemTypes.Container);
    public static ItemType CraftingMaterial = new(ItemTypes.CraftingMaterial);
    public static ItemType Gathering = new(ItemTypes.Gathering);
    public static ItemType Gizmo = new(ItemTypes.Gizmo);
    public static ItemType Key = new(ItemTypes.Key);
    public static ItemType MiniPet = new(ItemTypes.MiniPet);
    public static ItemType Tool = new(ItemTypes.Tool);
    public static ItemType Trait = new(ItemTypes.Trait);
    public static ItemType Trinket = new(ItemTypes.Trinket);
    public static ItemType Trophy = new(ItemTypes.Trophy);
    public static ItemType UpgradeComponent = new(ItemTypes.UpgradeComponent);
    public static ItemType Weapon = new(ItemTypes.Weapon);
    
    public string Value { get; }

    public ItemType(string value)
    {
        Value = value;
    }

    public static implicit operator ItemType(string value) => new(value);
}