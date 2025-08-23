namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public readonly struct SkillFlag : IEquatable<SkillFlag>
{
    public static readonly SkillFlag GroundTargeted = new("GroundTargeted");
    public static readonly SkillFlag NoUnderwater = new("NoUnderwater");

    public string Value { get; }

    private SkillFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkillFlag(string value) => new(value);

    public static implicit operator string(SkillFlag value) => value.Value;

    public bool Equals(SkillFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkillFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkillFlag left, SkillFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkillFlag left, SkillFlag right)
    {
        return !left.Equals(right);
    }
}
