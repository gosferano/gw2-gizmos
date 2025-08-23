namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public readonly struct AchievementRewardType : IEquatable<AchievementRewardType>
{
    public static readonly AchievementRewardType Coins = new AchievementRewardType("Coins");
    public static readonly AchievementRewardType Item = new AchievementRewardType("Item");
    public static readonly AchievementRewardType Mastery = new AchievementRewardType("Mastery");
    public static readonly AchievementRewardType Title = new AchievementRewardType("Title");

    public string Value { get; }

    private AchievementRewardType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AchievementRewardType(string value) => new(value);

    public bool Equals(AchievementRewardType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AchievementRewardType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AchievementRewardType left, AchievementRewardType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AchievementRewardType left, AchievementRewardType right)
    {
        return !left.Equals(right);
    }
}
