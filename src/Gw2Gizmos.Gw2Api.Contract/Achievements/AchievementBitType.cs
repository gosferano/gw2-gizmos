namespace Gw2Gizmos.Gw2Api.Contract.Achievements;

public struct AchievementBitType : IEquatable<AchievementBitType>
{
    public static readonly AchievementBitType Text = new AchievementBitType("Text");
    public static readonly AchievementBitType Item = new AchievementBitType("Item");
    public static readonly AchievementBitType Minipet = new AchievementBitType("Minipet");
    public static readonly AchievementBitType Skin = new AchievementBitType("Skin");

    public string Value { get; }

    private AchievementBitType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AchievementBitType(string value) => new(value);

    public bool Equals(AchievementBitType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AchievementBitType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AchievementBitType left, AchievementBitType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AchievementBitType left, AchievementBitType right)
    {
        return !left.Equals(right);
    }
}
