namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct ItemFlag : IEquatable<ItemFlag>
{
    public static readonly ItemFlag AccountBindOnUse = new("AccountBindOnUse");
    public static readonly ItemFlag AccountBound = new("AccountBound");
    public static readonly ItemFlag Attuned = new("Attuned");
    public static readonly ItemFlag BulkConsume = new("BulkConsume");
    public static readonly ItemFlag DeleteWarning = new("DeleteWarning");
    public static readonly ItemFlag HideSuffix = new("HideSuffix");
    public static readonly ItemFlag Infused = new("Infused");
    public static readonly ItemFlag MonsterOnly = new("MonsterOnly");
    public static readonly ItemFlag NoMysticForge = new("NoMysticForge");
    public static readonly ItemFlag NoSalvage = new("NoSalvage");
    public static readonly ItemFlag NoSell = new("NoSell");
    public static readonly ItemFlag NotUpgradeable = new("NotUpgradeable");
    public static readonly ItemFlag NoUnderwater = new("NoUnderwater");
    public static readonly ItemFlag SoulbindOnAcquire = new("SoulbindOnAcquire");
    public static readonly ItemFlag SoulBindOnUse = new("SoulBindOnUse");
    public static readonly ItemFlag Tonic = new("Tonic");
    public static readonly ItemFlag Unique = new("Unique");

    public string Value { get; }

    private ItemFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ItemFlag(string value) => new(value);

    public bool Equals(ItemFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ItemFlag left, ItemFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemFlag left, ItemFlag right)
    {
        return !left.Equals(right);
    }
}
