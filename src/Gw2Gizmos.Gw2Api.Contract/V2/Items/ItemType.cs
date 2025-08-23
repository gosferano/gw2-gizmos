namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct ItemType : IEquatable<ItemType>
{
    public static readonly ItemType Armor = new("Armor");
    public static readonly ItemType Back = new("Back");
    public static readonly ItemType Bag = new("Bag");
    public static readonly ItemType Consumable = new("Consumable");
    public static readonly ItemType Container = new("Container");
    public static readonly ItemType CraftingMaterial = new("CraftingMaterial");
    public static readonly ItemType Gathering = new("Gathering");
    public static readonly ItemType Gizmo = new("Gizmo");
    public static readonly ItemType JadeTechModule = new("JadeTechModule");
    public static readonly ItemType Key = new("Key");
    public static readonly ItemType MiniPet = new("MiniPet");
    public static readonly ItemType PowerCore = new("PowerCore");
    public static readonly ItemType Relic = new("Relic");
    public static readonly ItemType Tool = new("Tool");
    public static readonly ItemType Trait = new("Trait");
    public static readonly ItemType Trinket = new("Trinket");
    public static readonly ItemType Trophy = new("Trophy");
    public static readonly ItemType UpgradeComponent = new("UpgradeComponent");
    public static readonly ItemType Weapon = new("Weapon");

    public string Value { get; }

    private ItemType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ItemType(string value) => new(value);

    public static implicit operator string(ItemType value) => value.Value;

    public bool Equals(ItemType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ItemType left, ItemType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemType left, ItemType right)
    {
        return !left.Equals(right);
    }
}
