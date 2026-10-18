namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public readonly struct WvwRegion : IEquatable<WvwRegion>
{
    public static readonly WvwRegion Na = new WvwRegion("na");
    public static readonly WvwRegion Eu = new WvwRegion("eu");

    public string Value { get; }

    private WvwRegion(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WvwRegion(string value) => new(value);

    public static implicit operator string(WvwRegion value) => value.Value;

    public bool Equals(WvwRegion other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WvwRegion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WvwRegion left, WvwRegion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WvwRegion left, WvwRegion right)
    {
        return !left.Equals(right);
    }
}
