namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public readonly struct PvpSeasonLeaderboardSettingsTierType : IEquatable<PvpSeasonLeaderboardSettingsTierType>
{
    public static readonly PvpSeasonLeaderboardSettingsTierType Rank = new PvpSeasonLeaderboardSettingsTierType("Rank");

    public string Value { get; }

    private PvpSeasonLeaderboardSettingsTierType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpSeasonLeaderboardSettingsTierType(string value) => new(value);

    public static implicit operator string(PvpSeasonLeaderboardSettingsTierType value) => value.Value;

    public bool Equals(PvpSeasonLeaderboardSettingsTierType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpSeasonLeaderboardSettingsTierType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(
        PvpSeasonLeaderboardSettingsTierType left,
        PvpSeasonLeaderboardSettingsTierType right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        PvpSeasonLeaderboardSettingsTierType left,
        PvpSeasonLeaderboardSettingsTierType right
    )
    {
        return !left.Equals(right);
    }
}
