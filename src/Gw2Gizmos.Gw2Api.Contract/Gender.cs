namespace Gw2Gizmos.Gw2Api.Contract;

public struct Gender : IEquatable<Gender>
{
    public static readonly Gender Male = new Gender("Male");
    public static readonly Gender Female = new Gender("Female");

    public string Value { get; }

    private Gender(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator Gender(string value) => new(value);

    public static implicit operator string(Gender value) => value.Value;

    public bool Equals(Gender other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Gender other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Gender left, Gender right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Gender left, Gender right)
    {
        return !left.Equals(right);
    }
}
