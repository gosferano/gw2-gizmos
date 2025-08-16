namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct MasteryPointRegion : IEquatable<MasteryPointRegion>
{
    public static readonly MasteryPointRegion Tyria = new MasteryPointRegion("Tyria");
    public static readonly MasteryPointRegion Maguuma = new MasteryPointRegion("Maguuma");
    public static readonly MasteryPointRegion Desert = new MasteryPointRegion("Desert");
    public static readonly MasteryPointRegion Tundra = new MasteryPointRegion("Tundra");
    public static readonly MasteryPointRegion Jade = new MasteryPointRegion("Jade");
    public static readonly MasteryPointRegion Sky = new MasteryPointRegion("Sky");

    public string Value { get; }

    private MasteryPointRegion(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator MasteryPointRegion(string value) => new(value);

    public static implicit operator string(MasteryPointRegion value) => value.Value;

    public bool Equals(MasteryPointRegion other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is MasteryPointRegion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(MasteryPointRegion left, MasteryPointRegion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MasteryPointRegion left, MasteryPointRegion right)
    {
        return !left.Equals(right);
    }
}
