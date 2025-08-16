namespace Gw2Gizmos.Gw2Api.Contract.Skills;

public readonly struct ComboFieldType : IEquatable<ComboFieldType>
{
    public static readonly ComboFieldType Air = new("Air");
    public static readonly ComboFieldType Dark = new("Dark");
    public static readonly ComboFieldType Ethereal = new("Ethereal");
    public static readonly ComboFieldType Fire = new("Fire");
    public static readonly ComboFieldType Ice = new("Ice");
    public static readonly ComboFieldType Light = new("Light");
    public static readonly ComboFieldType Lightning = new("Lightning");
    public static readonly ComboFieldType Poison = new("Poison");
    public static readonly ComboFieldType Smoke = new("Smoke");
    public static readonly ComboFieldType Water = new("Water");

    public string Value { get; }

    private ComboFieldType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ComboFieldType(string value) => new(value);

    public static implicit operator string(ComboFieldType value) => value.Value;

    public bool Equals(ComboFieldType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComboFieldType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ComboFieldType left, ComboFieldType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComboFieldType left, ComboFieldType right)
    {
        return !left.Equals(right);
    }
}
