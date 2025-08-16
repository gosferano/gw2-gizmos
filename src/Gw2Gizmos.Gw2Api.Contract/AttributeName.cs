namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct AttributeName : IEquatable<AttributeName>
{
    public static readonly AttributeName AgonyResistance = new AttributeName("AgonyResistance");
    public static readonly AttributeName BoonDuration = new AttributeName("BoonDuration");
    public static readonly AttributeName ConditionDamage = new AttributeName("ConditionDamage");
    public static readonly AttributeName ConditionDuration = new AttributeName("ConditionDuration");
    public static readonly AttributeName CritDamage = new AttributeName("CritDamage");
    public static readonly AttributeName Healing = new AttributeName("Healing");
    public static readonly AttributeName Power = new AttributeName("Power");
    public static readonly AttributeName Precision = new AttributeName("Precision");
    public static readonly AttributeName Toughness = new AttributeName("Toughness");
    public static readonly AttributeName Vitality = new AttributeName("Vitality");

    public string Value { get; }

    private AttributeName(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AttributeName(string value) => new(value);

    public static implicit operator string(AttributeName value) => value.Value;

    public bool Equals(AttributeName other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AttributeName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AttributeName left, AttributeName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AttributeName left, AttributeName right)
    {
        return !left.Equals(right);
    }
}
