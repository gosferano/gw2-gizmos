namespace Gw2Gizmos.Gw2Api.Contract.V2;

public readonly struct SkillType : IEquatable<SkillType>
{
    public static readonly SkillType Profession = new("Profession");
    public static readonly SkillType Heal = new("Heal");
    public static readonly SkillType Utility = new("Utility");
    public static readonly SkillType Elite = new("Elite");
    public static readonly SkillType Weapon = new("Weapon");
    public static readonly SkillType Pet = new("Pet");
    public static readonly SkillType Bundle = new("Bundle");
    public static readonly SkillType Toolbelt = new("Toolbelt");
    public static readonly SkillType Transform = new("Transform");
    public static readonly SkillType Monster = new("Monster");

    public string Value { get; }

    private SkillType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkillType(string value) => new(value);

    public static implicit operator string(SkillType value) => value.Value;

    public bool Equals(SkillType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkillType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkillType left, SkillType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkillType left, SkillType right)
    {
        return !left.Equals(right);
    }
}
