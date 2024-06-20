namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public readonly struct PvpSeasonLeaderboardScoringType : IEquatable<PvpSeasonLeaderboardScoringType>
{
    public static readonly PvpSeasonLeaderboardScoringType Integer = new("Integer");

    public string Value { get; }

    private PvpSeasonLeaderboardScoringType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpSeasonLeaderboardScoringType(string value) => new(value);

    public static implicit operator string(PvpSeasonLeaderboardScoringType value) => value.Value;

    public bool Equals(PvpSeasonLeaderboardScoringType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpSeasonLeaderboardScoringType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpSeasonLeaderboardScoringType left, PvpSeasonLeaderboardScoringType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpSeasonLeaderboardScoringType left, PvpSeasonLeaderboardScoringType right)
    {
        return !left.Equals(right);
    }
}
