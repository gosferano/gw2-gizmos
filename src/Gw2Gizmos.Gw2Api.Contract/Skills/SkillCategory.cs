namespace Gw2Gizmos.Gw2Api.Contract.Skills;

public readonly struct SkillCategory : IEquatable<SkillCategory>
{
    public static readonly SkillCategory Arcane = new("Arcane");
    public static readonly SkillCategory Banner = new("Banner");
    public static readonly SkillCategory Burst = new("Burst");
    public static readonly SkillCategory Cantrip = new("Cantrip");
    public static readonly SkillCategory CelestialAvatar = new("CelestialAvatar");
    public static readonly SkillCategory Clone = new("Clone");
    public static readonly SkillCategory Conjure = new("Conjure");
    public static readonly SkillCategory Consecration = new("Consecration");
    public static readonly SkillCategory Corruption = new("Corruption");
    public static readonly SkillCategory Deception = new("Deception");
    public static readonly SkillCategory DualWield = new("DualWield");
    public static readonly SkillCategory Elixir = new("Elixir");
    public static readonly SkillCategory Gadget = new("Gadget");
    public static readonly SkillCategory Glamour = new("Glamour");
    public static readonly SkillCategory Glyph = new("Glyph");
    public static readonly SkillCategory Kit = new("Kit");
    public static readonly SkillCategory LegendaryAssassin = new("LegendaryAssassin");
    public static readonly SkillCategory LegendaryDemon = new("LegendaryDemon");
    public static readonly SkillCategory LegendaryDragon = new("LegendaryDragon");
    public static readonly SkillCategory LegendaryDwarf = new("LegendaryDwarf");
    public static readonly SkillCategory Manipulation = new("Manipulation");
    public static readonly SkillCategory Mantra = new("Mantra");
    public static readonly SkillCategory Mark = new("Mark");
    public static readonly SkillCategory Meditation = new("Meditation");
    public static readonly SkillCategory Minion = new("Minion");
    public static readonly SkillCategory Overload = new("Overload");
    public static readonly SkillCategory Phantasm = new("Phantasm");
    public static readonly SkillCategory Physical = new("Physical");
    public static readonly SkillCategory PrimalBurst = new("PrimalBurst");
    public static readonly SkillCategory Rage = new("Rage");
    public static readonly SkillCategory Shout = new("Shout");
    public static readonly SkillCategory Signet = new("Signet");
    public static readonly SkillCategory Spectral = new("Spectral");
    public static readonly SkillCategory Spirit = new("Spirit");
    public static readonly SkillCategory SpiritWeapon = new("SpiritWeapon");
    public static readonly SkillCategory Stance = new("Stance");
    public static readonly SkillCategory StealthAttack = new("StealthAttack");
    public static readonly SkillCategory Survival = new("Survival");
    public static readonly SkillCategory Symbol = new("Symbol");
    public static readonly SkillCategory Transform = new("Transform");
    public static readonly SkillCategory Trap = new("Trap");
    public static readonly SkillCategory Trick = new("Trick");
    public static readonly SkillCategory Turret = new("Turret");
    public static readonly SkillCategory Venom = new("Venom");
    public static readonly SkillCategory Virtue = new("Virtue");
    public static readonly SkillCategory Ward = new("Ward");
    public static readonly SkillCategory Well = new("Well");

    public string Value { get; }

    private SkillCategory(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkillCategory(string value) => new(value);

    public static implicit operator string(SkillCategory value) => value.Value;

    public bool Equals(SkillCategory other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkillCategory other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkillCategory left, SkillCategory right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkillCategory left, SkillCategory right)
    {
        return !left.Equals(right);
    }
}
