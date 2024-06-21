namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct SkillSlot : IEquatable<SkillSlot>
{
    public static readonly SkillSlot Weapon1 = new("Weapon_1");
    public static readonly SkillSlot Weapon2 = new("Weapon_2");
    public static readonly SkillSlot Weapon3 = new("Weapon_3");
    public static readonly SkillSlot Weapon4 = new("Weapon_4");
    public static readonly SkillSlot Weapon5 = new("Weapon_5");
    public static readonly SkillSlot Heal = new("Heal");
    public static readonly SkillSlot Utility = new("Utility");
    public static readonly SkillSlot Elite = new("Elite");
    public static readonly SkillSlot Profession1 = new("Profession_1");
    public static readonly SkillSlot Profession2 = new("Profession_2");
    public static readonly SkillSlot Profession3 = new("Profession_3");
    public static readonly SkillSlot Profession4 = new("Profession_4");
    public static readonly SkillSlot Profession5 = new("Profession_5");
    public static readonly SkillSlot Downed1 = new("Downed_1");
    public static readonly SkillSlot Downed2 = new("Downed_2");
    public static readonly SkillSlot Downed3 = new("Downed_3");
    public static readonly SkillSlot Downed4 = new("Downed_4");
    public static readonly SkillSlot Pet = new("Pet");
    public static readonly SkillSlot Toolbelt = new("Toolbelt");
    public static readonly SkillSlot Transform1 = new("Transform_1");

    public string Value { get; }

    private SkillSlot(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkillSlot(string value) => new(value);

    public static implicit operator string(SkillSlot value) => value.Value;

    public bool Equals(SkillSlot other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkillSlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkillSlot left, SkillSlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkillSlot left, SkillSlot right)
    {
        return !left.Equals(right);
    }
}
