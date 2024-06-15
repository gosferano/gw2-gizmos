namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct ItemBinding : IEquatable<ItemBinding>
{
    public static readonly ItemBinding Account = new ItemBinding("Account");
    public static readonly ItemBinding Character = new ItemBinding("Character");

    public string Value { get; }

    private ItemBinding(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ItemBinding(string value) => new(value);

    public bool Equals(ItemBinding other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemBinding other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ItemBinding left, ItemBinding right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemBinding left, ItemBinding right)
    {
        return !left.Equals(right);
    }
}
