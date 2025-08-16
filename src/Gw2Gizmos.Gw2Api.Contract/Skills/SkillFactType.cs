namespace Gw2Gizmos.Gw2Api.Contract.Skills;

public readonly struct SkillFactType : IEquatable<SkillFactType>
{
    public static readonly SkillFactType AttributeAdjust = new("AttributeAdjust");
    public static readonly SkillFactType Buff = new("Buff");
    public static readonly SkillFactType ComboField = new("ComboField");
    public static readonly SkillFactType ComboFinisher = new("ComboFinisher");
    public static readonly SkillFactType Damage = new("Damage");
    public static readonly SkillFactType Distance = new("Distance");
    public static readonly SkillFactType Duration = new("Duration");
    public static readonly SkillFactType Heal = new("Heal");
    public static readonly SkillFactType HealingAdjust = new("HealingAdjust");
    public static readonly SkillFactType NoData = new("NoData");
    public static readonly SkillFactType Number = new("Number");
    public static readonly SkillFactType Percent = new("Percent");
    public static readonly SkillFactType PrefixedBuff = new("PrefixedBuff");
    public static readonly SkillFactType Radius = new("Radius");
    public static readonly SkillFactType Range = new("Range");
    public static readonly SkillFactType Recharge = new("Recharge");
    public static readonly SkillFactType StunBreak = new("StunBreak");
    public static readonly SkillFactType Time = new("Time");
    public static readonly SkillFactType Unblockable = new("Unblockable");

    public string Value { get; }

    private SkillFactType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkillFactType(string value) => new(value);

    public static implicit operator string(SkillFactType value) => value.Value;

    public bool Equals(SkillFactType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkillFactType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkillFactType left, SkillFactType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkillFactType left, SkillFactType right)
    {
        return !left.Equals(right);
    }
}
