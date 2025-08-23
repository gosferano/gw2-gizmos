namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public readonly struct PvpSeasonFlag : IEquatable<PvpSeasonFlag>
{
    public static readonly PvpSeasonFlag CanLosePoints = new PvpSeasonFlag("CanLosePoints");
    public static readonly PvpSeasonFlag CanLoseTiers = new PvpSeasonFlag("CanLoseTiers");
    public static readonly PvpSeasonFlag Repeatable = new PvpSeasonFlag("Repeatable");

    public string Value { get; }

    private PvpSeasonFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpSeasonFlag(string value) => new(value);

    public static implicit operator string(PvpSeasonFlag value) => value.Value;

    public bool Equals(PvpSeasonFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpSeasonFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpSeasonFlag left, PvpSeasonFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpSeasonFlag left, PvpSeasonFlag right)
    {
        return !left.Equals(right);
    }
}
