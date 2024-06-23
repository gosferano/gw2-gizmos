namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public readonly struct WvwMapType : IEquatable<WvwMapType>
{
    public static readonly WvwMapType RedHome = new WvwMapType("RedHome");
    public static readonly WvwMapType BlueHome = new WvwMapType("BlueHome");
    public static readonly WvwMapType GreenHome = new WvwMapType("GreenHome");
    public static readonly WvwMapType Center = new WvwMapType("Center");
    public static readonly WvwMapType EdgeOfTheMists = new WvwMapType("EdgeOfTheMists");

    public string Value { get; }

    private WvwMapType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WvwMapType(string value) => new(value);

    public static implicit operator string(WvwMapType value) => value.Value;

    public bool Equals(WvwMapType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WvwMapType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WvwMapType left, WvwMapType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WvwMapType left, WvwMapType right)
    {
        return !left.Equals(right);
    }
}
