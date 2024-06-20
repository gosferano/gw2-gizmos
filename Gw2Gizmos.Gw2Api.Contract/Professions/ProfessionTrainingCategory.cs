namespace Gw2Gizmos.Gw2Api.Contract.Professions;

public readonly struct ProfessionTrainingCategory : IEquatable<ProfessionTrainingCategory>
{
    public static readonly ProfessionTrainingCategory Skills = new("Skills");
    public static readonly ProfessionTrainingCategory Specializations = new("Specializations");
    public static readonly ProfessionTrainingCategory EliteSpecializations = new("EliteSpecializations");

    public string Value { get; }

    private ProfessionTrainingCategory(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ProfessionTrainingCategory(string value) => new(value);

    public static implicit operator string(ProfessionTrainingCategory value) => value.Value;

    public bool Equals(ProfessionTrainingCategory other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProfessionTrainingCategory other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ProfessionTrainingCategory left, ProfessionTrainingCategory right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProfessionTrainingCategory left, ProfessionTrainingCategory right)
    {
        return !left.Equals(right);
    }
}
