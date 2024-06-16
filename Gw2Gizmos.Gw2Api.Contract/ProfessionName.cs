namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct ProfessionName : IEquatable<ProfessionName>
{
    public static readonly ProfessionName Elementalist = new ProfessionName("Elementalist");
    public static readonly ProfessionName Engineer = new ProfessionName("Engineer");
    public static readonly ProfessionName Guardian = new ProfessionName("Guardian");
    public static readonly ProfessionName Mesmer = new ProfessionName("Mesmer");
    public static readonly ProfessionName Necromancer = new ProfessionName("Necromancer");
    public static readonly ProfessionName Ranger = new ProfessionName("Ranger");
    public static readonly ProfessionName Revenant = new ProfessionName("Revenant");
    public static readonly ProfessionName Thief = new ProfessionName("Thief");
    public static readonly ProfessionName Warrior = new ProfessionName("Warrior");

    public string Value { get; }

    private ProfessionName(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ProfessionName(string value) => new(value);

    public static implicit operator string(ProfessionName value) => value.Value;

    public bool Equals(ProfessionName other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProfessionName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ProfessionName left, ProfessionName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProfessionName left, ProfessionName right)
    {
        return !left.Equals(right);
    }
}
