namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The character's active elite specialization, as the numeric specialization id MumbleLink's identity reports
/// (0 when no elite spec is equipped). There are many ids and they grow with each expansion, so this carries the
/// raw <see cref="Value"/> with implicit conversions rather than a named instance per spec — callers resolve the
/// name against the GW2 API's specializations endpoint when display is needed.
/// </summary>
public readonly struct EliteSpecialization : IEquatable<EliteSpecialization>
{
    /// <summary>No elite specialization equipped.</summary>
    public static readonly EliteSpecialization None = new(0);

    public int Value { get; }

    private EliteSpecialization(int value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator EliteSpecialization(int value) => new(value);

    public static implicit operator int(EliteSpecialization value) => value.Value;

    public bool Equals(EliteSpecialization other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is EliteSpecialization other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(EliteSpecialization left, EliteSpecialization right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EliteSpecialization left, EliteSpecialization right)
    {
        return !left.Equals(right);
    }
}
