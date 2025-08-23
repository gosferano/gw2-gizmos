namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public struct UpgradeComponentType : IEquatable<UpgradeComponentType>
{
    public static readonly UpgradeComponentType Default = new("Default");
    public static readonly UpgradeComponentType Gem = new("Gem");
    public static readonly UpgradeComponentType Rune = new("Rune");
    public static readonly UpgradeComponentType Sigil = new("Sigil");

    public string Value { get; }

    private UpgradeComponentType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator UpgradeComponentType(string value) => new(value);

    public bool Equals(UpgradeComponentType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is UpgradeComponentType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(UpgradeComponentType left, UpgradeComponentType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UpgradeComponentType left, UpgradeComponentType right)
    {
        return !left.Equals(right);
    }
}
