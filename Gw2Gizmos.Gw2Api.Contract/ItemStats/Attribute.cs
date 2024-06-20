namespace Gw2Gizmos.Gw2Api.Contract.ItemStats;

public readonly struct Attribute : IEquatable<Attribute>
{
    public static readonly Attribute AgonyResistance = new Attribute("AgonyResistance");
    public static readonly Attribute BoonDuration = new Attribute("BoonDuration");
    public static readonly Attribute ConditionDamage = new Attribute("ConditionDamage");
    public static readonly Attribute ConditionDuration = new Attribute("ConditionDuration");
    public static readonly Attribute CritDamage = new Attribute("CritDamage");
    public static readonly Attribute Healing = new Attribute("Healing");
    public static readonly Attribute Power = new Attribute("Power");
    public static readonly Attribute Precision = new Attribute("Precision");
    public static readonly Attribute Toughness = new Attribute("Toughness");
    public static readonly Attribute Vitality = new Attribute("Vitality");

    public string Value { get; }

    private Attribute(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator Attribute(string value) => new(value);

    public static implicit operator string(Attribute value) => value.Value;

    public bool Equals(Attribute other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Attribute other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Attribute left, Attribute right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Attribute left, Attribute right)
    {
        return !left.Equals(right);
    }
}
