namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public readonly struct AchievementFlag : IEquatable<AchievementFlag>
{
    public static readonly AchievementFlag Pvp = new AchievementFlag("Pvp");
    public static readonly AchievementFlag CategoryDisplay = new AchievementFlag("CategoryDisplay");
    public static readonly AchievementFlag MoveToTop = new AchievementFlag("MoveToTop");
    public static readonly AchievementFlag IgnoreNearlyComplete = new AchievementFlag("IgnoreNearlyComplete");
    public static readonly AchievementFlag Repeatable = new AchievementFlag("Repeatable");
    public static readonly AchievementFlag Hidden = new AchievementFlag("Hidden");
    public static readonly AchievementFlag RequiresUnlock = new AchievementFlag("RequiresUnlock");
    public static readonly AchievementFlag RepairOnLogin = new AchievementFlag("RepairOnLogin");
    public static readonly AchievementFlag Daily = new AchievementFlag("Daily");
    public static readonly AchievementFlag Weekly = new AchievementFlag("Weekly");
    public static readonly AchievementFlag Monthly = new AchievementFlag("Monthly");
    public static readonly AchievementFlag Permanent = new AchievementFlag("Permanent");

    public string Value { get; }

    private AchievementFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AchievementFlag(string value) => new(value);

    public bool Equals(AchievementFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AchievementFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AchievementFlag left, AchievementFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AchievementFlag left, AchievementFlag right)
    {
        return !left.Equals(right);
    }
}
