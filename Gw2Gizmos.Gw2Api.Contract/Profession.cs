namespace Gw2Gizmos.Gw2Api.Contract;

public struct Profession : IEquatable<Profession>
{
    public static readonly Profession Elementalist = new Profession("Elementalist");
    public static readonly Profession Engineer = new Profession("Engineer");
    public static readonly Profession Guardian = new Profession("Guardian");
    public static readonly Profession Mesmer = new Profession("Mesmer");
    public static readonly Profession Necromancer = new Profession("Necromancer");
    public static readonly Profession Ranger = new Profession("Ranger");
    public static readonly Profession Revenant = new Profession("Revenant");
    public static readonly Profession Thief = new Profession("Thief");
    public static readonly Profession Warrior = new Profession("Warrior");

    public string Value { get; }

    private Profession(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator Profession(string value) => new(value);

    public static implicit operator string(Profession value) => value.Value;

    public bool Equals(Profession other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Profession other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Profession left, Profession right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Profession left, Profession right)
    {
        return !left.Equals(right);
    }
}
