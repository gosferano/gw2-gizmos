namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public readonly struct WvwOwner : IEquatable<WvwOwner>
{
    public static readonly WvwOwner Red = new("Red");
    public static readonly WvwOwner Blue = new("Blue");
    public static readonly WvwOwner Green = new("Green");

    public string Value { get; }

    private WvwOwner(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WvwOwner(string value) => new(value);

    public static implicit operator string(WvwOwner value) => value.Value;

    public bool Equals(WvwOwner other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WvwOwner other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WvwOwner left, WvwOwner right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WvwOwner left, WvwOwner right)
    {
        return !left.Equals(right);
    }
}
