namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct CraftingDisciplineName
{
    public static readonly CraftingDisciplineName Armorsmith = new("Armorsmith");
    public static readonly CraftingDisciplineName Artificer = new("Artificer");
    public static readonly CraftingDisciplineName Chef = new("Chef");
    public static readonly CraftingDisciplineName Huntsman = new("Huntsman");
    public static readonly CraftingDisciplineName Jeweler = new("Jeweler");
    public static readonly CraftingDisciplineName Leatherworker = new("Leatherworker");
    public static readonly CraftingDisciplineName Scribe = new("Scribe");
    public static readonly CraftingDisciplineName Tailor = new("Tailor");
    public static readonly CraftingDisciplineName Weaponsmith = new("Weaponsmith");

    public string Value { get; }

    private CraftingDisciplineName(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator CraftingDisciplineName(string value) => new(value);

    public static implicit operator string(CraftingDisciplineName value) => value.Value;

    public bool Equals(CraftingDisciplineName other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CraftingDisciplineName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CraftingDisciplineName left, CraftingDisciplineName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CraftingDisciplineName left, CraftingDisciplineName right)
    {
        return !left.Equals(right);
    }
}
