namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public readonly struct PvpSeasonLeaderboardScoringOrder : IEquatable<PvpSeasonLeaderboardScoringOrder>
{
    public static readonly PvpSeasonLeaderboardScoringOrder LessIsBetter = new("LessIsBetter");
    public static readonly PvpSeasonLeaderboardScoringOrder MoreIsBetter = new("MoreIsBetter");

    public string Value { get; }

    private PvpSeasonLeaderboardScoringOrder(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpSeasonLeaderboardScoringOrder(string value) => new(value);

    public static implicit operator string(PvpSeasonLeaderboardScoringOrder value) => value.Value;

    public bool Equals(PvpSeasonLeaderboardScoringOrder other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpSeasonLeaderboardScoringOrder other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpSeasonLeaderboardScoringOrder left, PvpSeasonLeaderboardScoringOrder right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpSeasonLeaderboardScoringOrder left, PvpSeasonLeaderboardScoringOrder right)
    {
        return !left.Equals(right);
    }
}
