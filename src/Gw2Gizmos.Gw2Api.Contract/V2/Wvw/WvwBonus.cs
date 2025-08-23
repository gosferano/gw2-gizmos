namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public readonly struct WvwBonus : IEquatable<WvwBonus>
{
    public static readonly WvwBonus Bloodlust = new("Bloodlust");

    public string Value { get; }

    private WvwBonus(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WvwBonus(string value) => new(value);

    public static implicit operator string(WvwBonus value) => value.Value;

    public bool Equals(WvwBonus other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WvwBonus other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WvwBonus left, WvwBonus right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WvwBonus left, WvwBonus right)
    {
        return !left.Equals(right);
    }
}
