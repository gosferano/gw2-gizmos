namespace Gw2Gizmos.Gw2Api.Contract.Skills;

public readonly struct ComboFinisherType : IEquatable<ComboFinisherType>
{
    public static readonly ComboFinisherType Blast = new("Blast");
    public static readonly ComboFinisherType Leap = new("Leap");
    public static readonly ComboFinisherType Projectile = new("Projectile");
    public static readonly ComboFinisherType Whirl = new("Whirl");

    public string Value { get; }

    private ComboFinisherType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ComboFinisherType(string value) => new(value);

    public static implicit operator string(ComboFinisherType value) => value.Value;

    public bool Equals(ComboFinisherType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComboFinisherType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ComboFinisherType left, ComboFinisherType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComboFinisherType left, ComboFinisherType right)
    {
        return !left.Equals(right);
    }
}
