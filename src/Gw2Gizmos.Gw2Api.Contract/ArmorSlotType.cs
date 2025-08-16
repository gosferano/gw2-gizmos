namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct ArmorSlotType : IEquatable<ArmorSlotType>
{
    public static readonly ArmorSlotType HelmAquatic = new("HelmAquatic");
    public static readonly ArmorSlotType Backpack = new("Backpack");
    public static readonly ArmorSlotType Coat = new("Coat");
    public static readonly ArmorSlotType Boots = new("Boots");
    public static readonly ArmorSlotType Gloves = new("Gloves");
    public static readonly ArmorSlotType Helm = new("Helm");
    public static readonly ArmorSlotType Leggings = new("Leggings");
    public static readonly ArmorSlotType Shoulders = new("Shoulders");

    public string Value { get; }

    private ArmorSlotType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ArmorSlotType(string value) => new(value);

    public static implicit operator string(ArmorSlotType value) => value.Value;

    public bool Equals(ArmorSlotType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArmorSlotType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ArmorSlotType left, ArmorSlotType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArmorSlotType left, ArmorSlotType right)
    {
        return !left.Equals(right);
    }
}
