namespace Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

public readonly struct HomesteadGlyphSlot : IEquatable<HomesteadGlyphSlot>
{
    public static readonly HomesteadGlyphSlot Harvesting = new HomesteadGlyphSlot("harvesting");
    public static readonly HomesteadGlyphSlot Logging = new HomesteadGlyphSlot("logging");
    public static readonly HomesteadGlyphSlot Mining = new HomesteadGlyphSlot("mining");

    public string Value { get; }

    private HomesteadGlyphSlot(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator HomesteadGlyphSlot(string value) => new(value);

    public static implicit operator string(HomesteadGlyphSlot value) => value.Value;

    public bool Equals(HomesteadGlyphSlot other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is HomesteadGlyphSlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(HomesteadGlyphSlot left, HomesteadGlyphSlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HomesteadGlyphSlot left, HomesteadGlyphSlot right)
    {
        return !left.Equals(right);
    }
}
