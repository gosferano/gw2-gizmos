namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct GatheringType : IEquatable<GatheringType>
{
    public static readonly GatheringType Foraging = new GatheringType("Foraging");
    public static readonly GatheringType Logging = new GatheringType("Logging");
    public static readonly GatheringType Mining = new GatheringType("Mining");
    public static readonly GatheringType Bait = new GatheringType("Bait");
    public static readonly GatheringType Lure = new GatheringType("Lure");

    public string Value { get; }

    private GatheringType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator GatheringType(string value) => new(value);

    public bool Equals(GatheringType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is GatheringType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(GatheringType left, GatheringType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GatheringType left, GatheringType right)
    {
        return !left.Equals(right);
    }
}
