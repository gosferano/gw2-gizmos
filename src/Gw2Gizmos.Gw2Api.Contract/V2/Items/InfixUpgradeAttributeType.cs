namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct InfixUpgradeAttributeType : IEquatable<InfixUpgradeAttributeType>
{
    public static readonly InfixUpgradeAttributeType AgonyResistance = new("AgonyResistance");
    public static readonly InfixUpgradeAttributeType BoonDuration = new("BoonDuration");
    public static readonly InfixUpgradeAttributeType ConditionDamage = new("ConditionDamage");
    public static readonly InfixUpgradeAttributeType ConditionDuration = new("ConditionDuration");
    public static readonly InfixUpgradeAttributeType CritDamage = new("CritDamage");
    public static readonly InfixUpgradeAttributeType Ferocity = new("Ferocity");
    public static readonly InfixUpgradeAttributeType Healing = new("Healing");
    public static readonly InfixUpgradeAttributeType Power = new("Power");
    public static readonly InfixUpgradeAttributeType Precision = new("Precision");
    public static readonly InfixUpgradeAttributeType Toughness = new("Toughness");
    public static readonly InfixUpgradeAttributeType Vitality = new("Vitality");

    public string Value { get; }

    private InfixUpgradeAttributeType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator InfixUpgradeAttributeType(string value) => new(value);

    public static implicit operator string(InfixUpgradeAttributeType value) => value.Value;

    public bool Equals(InfixUpgradeAttributeType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is InfixUpgradeAttributeType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(InfixUpgradeAttributeType left, InfixUpgradeAttributeType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InfixUpgradeAttributeType left, InfixUpgradeAttributeType right)
    {
        return !left.Equals(right);
    }
}
