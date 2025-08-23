namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public readonly struct ProfessionWeaponFlag : IEquatable<ProfessionWeaponFlag>
{
    public static readonly ProfessionWeaponFlag Mainhand = new ProfessionWeaponFlag("Mainhand");
    public static readonly ProfessionWeaponFlag Offhand = new ProfessionWeaponFlag("Offhand");
    public static readonly ProfessionWeaponFlag TwoHand = new ProfessionWeaponFlag("TwoHand");
    public static readonly ProfessionWeaponFlag Aquatic = new ProfessionWeaponFlag("Aquatic");

    public string Value { get; }

    private ProfessionWeaponFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ProfessionWeaponFlag(string value) => new(value);

    public static implicit operator string(ProfessionWeaponFlag value) => value.Value;

    public bool Equals(ProfessionWeaponFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProfessionWeaponFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ProfessionWeaponFlag left, ProfessionWeaponFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProfessionWeaponFlag left, ProfessionWeaponFlag right)
    {
        return !left.Equals(right);
    }
}
