namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public readonly struct TraitFactType : IEquatable<TraitFactType>
{
    public static readonly TraitFactType AttributeAdjust = new("AttributeAdjust");
    public static readonly TraitFactType Buff = new("Buff");
    public static readonly TraitFactType BuffConversion = new("BuffConversion");
    public static readonly TraitFactType ComboField = new("ComboField");
    public static readonly TraitFactType ComboFinisher = new("ComboFinisher");
    public static readonly TraitFactType Damage = new("Damage");
    public static readonly TraitFactType Distance = new("Distance");
    public static readonly TraitFactType NoData = new("NoData");
    public static readonly TraitFactType Number = new("Number");
    public static readonly TraitFactType Percent = new("Percent");
    public static readonly TraitFactType PrefixedBuff = new("PrefixedBuff");
    public static readonly TraitFactType Radius = new("Radius");
    public static readonly TraitFactType Range = new("Range");
    public static readonly TraitFactType Recharge = new("Recharge");
    public static readonly TraitFactType Time = new("Time");
    public static readonly TraitFactType Unblockable = new("Unblockable");

    public string Value { get; }

    private TraitFactType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator TraitFactType(string value) => new(value);

    public static implicit operator string(TraitFactType value) => value.Value;

    public bool Equals(TraitFactType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TraitFactType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TraitFactType left, TraitFactType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TraitFactType left, TraitFactType right)
    {
        return !left.Equals(right);
    }
}
