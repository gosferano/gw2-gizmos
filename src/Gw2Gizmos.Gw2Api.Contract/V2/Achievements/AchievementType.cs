namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public readonly struct AchievementType : IEquatable<AchievementType>
{
    public static readonly AchievementType Default = new AchievementType("Default");
    public static readonly AchievementType ItemSet = new AchievementType("ItemSet");

    public string Value { get; }

    private AchievementType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AchievementType(string value) => new(value);

    public bool Equals(AchievementType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AchievementType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AchievementType left, AchievementType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AchievementType left, AchievementType right)
    {
        return !left.Equals(right);
    }
}
