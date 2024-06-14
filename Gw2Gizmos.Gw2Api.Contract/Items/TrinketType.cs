namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct TrinketType : IEquatable<TrinketType>
{
    public static readonly TrinketType Accessory = new("Accessory");
    public static readonly TrinketType Amulet = new("Amulet");
    public static readonly TrinketType Ring = new("Ring");

    public string Value { get; }

    private TrinketType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator TrinketType(string value) => new(value);

    public bool Equals(TrinketType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TrinketType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TrinketType left, TrinketType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TrinketType left, TrinketType right)
    {
        return !left.Equals(right);
    }
}
