namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public readonly struct PvpRatingType : IEquatable<PvpRatingType>
{
    public static readonly PvpRatingType Ranked = new("Ranked");
    public static readonly PvpRatingType Unranked = new("Unranked");
    public static readonly PvpRatingType None = new("None");

    public string Value { get; }

    private PvpRatingType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpRatingType(string value) => new(value);

    public static implicit operator string(PvpRatingType value) => value.Value;

    public bool Equals(PvpRatingType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpRatingType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpRatingType left, PvpRatingType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpRatingType left, PvpRatingType right)
    {
        return !left.Equals(right);
    }
}
