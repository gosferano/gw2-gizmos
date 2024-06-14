namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct ItemRarity : IEquatable<ItemRarity>
{
    public static readonly ItemRarity Junk = new("Junk");
    public static readonly ItemRarity Basic = new("Basic");
    public static readonly ItemRarity Fine = new("Fine");
    public static readonly ItemRarity Masterwork = new("Masterwork");
    public static readonly ItemRarity Rare = new("Rare");
    public static readonly ItemRarity Exotic = new("Exotic");
    public static readonly ItemRarity Ascended = new("Ascended");
    public static readonly ItemRarity Legendary = new("Legendary");

    public string Value { get; }

    private ItemRarity(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ItemRarity(string value) => new(value);

    public bool Equals(ItemRarity other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemRarity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ItemRarity left, ItemRarity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemRarity left, ItemRarity right)
    {
        return !left.Equals(right);
    }
}
