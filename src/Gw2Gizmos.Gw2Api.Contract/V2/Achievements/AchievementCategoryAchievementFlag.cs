namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public readonly struct AchievementCategoryAchievementFlag : IEquatable<AchievementCategoryAchievementFlag>
{
    public static readonly AchievementCategoryAchievementFlag PvE = new AchievementCategoryAchievementFlag("PvE");
    public static readonly AchievementCategoryAchievementFlag PvP = new AchievementCategoryAchievementFlag("PvP");
    public static readonly AchievementCategoryAchievementFlag WvW = new AchievementCategoryAchievementFlag("WvW");
    public static readonly AchievementCategoryAchievementFlag SpecialEvent = new AchievementCategoryAchievementFlag(
        "SpecialEvent"
    );

    public string Value { get; }

    private AchievementCategoryAchievementFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AchievementCategoryAchievementFlag(string value) => new(value);

    public bool Equals(AchievementCategoryAchievementFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AchievementCategoryAchievementFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AchievementCategoryAchievementFlag left, AchievementCategoryAchievementFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AchievementCategoryAchievementFlag left, AchievementCategoryAchievementFlag right)
    {
        return !left.Equals(right);
    }
}
