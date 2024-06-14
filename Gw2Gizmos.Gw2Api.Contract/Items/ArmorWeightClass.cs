namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct ArmorWeightClass : IEquatable<ArmorWeightClass>
{
    public static readonly ArmorWeightClass Heavy = new("Heavy");
    public static readonly ArmorWeightClass Medium = new("Medium");
    public static readonly ArmorWeightClass Light = new("Light");
    public static readonly ArmorWeightClass Clothing = new("Clothing");

    public string Value { get; }

    private ArmorWeightClass(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ArmorWeightClass(string value) => new(value);

    public bool Equals(ArmorWeightClass other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArmorWeightClass other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ArmorWeightClass left, ArmorWeightClass right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArmorWeightClass left, ArmorWeightClass right)
    {
        return !left.Equals(right);
    }
}
