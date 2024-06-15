namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct CraftingDiscipline
{
    public static readonly CraftingDiscipline Armorsmith = new("Armorsmith");
    public static readonly CraftingDiscipline Artificer = new("Artificer");
    public static readonly CraftingDiscipline Chef = new("Chef");
    public static readonly CraftingDiscipline Huntsman = new("Huntsman");
    public static readonly CraftingDiscipline Jeweler = new("Jeweler");
    public static readonly CraftingDiscipline Leatherworker = new("Leatherworker");
    public static readonly CraftingDiscipline Scribe = new("Scribe");
    public static readonly CraftingDiscipline Tailor = new("Tailor");
    public static readonly CraftingDiscipline Weaponsmith = new("Weaponsmith");

    public string Value { get; }

    private CraftingDiscipline(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator CraftingDiscipline(string value) => new(value);

    public static implicit operator string(CraftingDiscipline value) => value.Value;

    public bool Equals(CraftingDiscipline other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CraftingDiscipline other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CraftingDiscipline left, CraftingDiscipline right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CraftingDiscipline left, CraftingDiscipline right)
    {
        return !left.Equals(right);
    }
}
