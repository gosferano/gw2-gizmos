namespace Gw2Gizmos.Gw2Api.Contract.Professions;

public readonly struct ProfessionFlag : IEquatable<ProfessionFlag>
{
    public static readonly ProfessionFlag NoRacialSkills = new ProfessionFlag("NoRacialSkills");
    public static readonly ProfessionFlag NoWeaponSwap = new ProfessionFlag("NoWeaponSwap");

    public string Value { get; }

    private ProfessionFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ProfessionFlag(string value) => new(value);

    public static implicit operator string(ProfessionFlag value) => value.Value;

    public bool Equals(ProfessionFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProfessionFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ProfessionFlag left, ProfessionFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProfessionFlag left, ProfessionFlag right)
    {
        return !left.Equals(right);
    }
}
