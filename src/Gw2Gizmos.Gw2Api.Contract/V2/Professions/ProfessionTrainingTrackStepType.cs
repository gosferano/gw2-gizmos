namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public readonly struct ProfessionTrainingTrackStepType : IEquatable<ProfessionTrainingTrackStepType>
{
    public static readonly ProfessionTrainingTrackStepType Skill = new("Skill");
    public static readonly ProfessionTrainingTrackStepType Trait = new("Trait");

    public string Value { get; }

    private ProfessionTrainingTrackStepType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ProfessionTrainingTrackStepType(string value) => new(value);

    public static implicit operator string(ProfessionTrainingTrackStepType value) => value.Value;

    public bool Equals(ProfessionTrainingTrackStepType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProfessionTrainingTrackStepType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ProfessionTrainingTrackStepType left, ProfessionTrainingTrackStepType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProfessionTrainingTrackStepType left, ProfessionTrainingTrackStepType right)
    {
        return !left.Equals(right);
    }
}
