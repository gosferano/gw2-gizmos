namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct WeaponType : IEquatable<WeaponType>
{
    public static readonly WeaponType None = new("None");
    public static readonly WeaponType Axe = new("Axe");
    public static readonly WeaponType Dagger = new("Dagger");
    public static readonly WeaponType Focus = new("Focus");
    public static readonly WeaponType Greatsword = new("Greatsword");
    public static readonly WeaponType Hammer = new("Hammer");
    public static readonly WeaponType Harpoon = new("Harpoon");
    public static readonly WeaponType LongBow = new("LongBow");
    public static readonly WeaponType Mace = new("Mace");
    public static readonly WeaponType Pistol = new("Pistol");
    public static readonly WeaponType Rifle = new("Rifle");
    public static readonly WeaponType Scepter = new("Scepter");
    public static readonly WeaponType Shield = new("Shield");
    public static readonly WeaponType ShortBow = new("ShortBow");
    public static readonly WeaponType Speargun = new("Speargun");
    public static readonly WeaponType Staff = new("Staff");
    public static readonly WeaponType Sword = new("Sword");
    public static readonly WeaponType Torch = new("Torch");
    public static readonly WeaponType Trident = new("Trident");
    public static readonly WeaponType Warhorn = new("Warhorn");

    public string Value { get; }

    private WeaponType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WeaponType(string value) => new(value);

    public bool Equals(WeaponType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WeaponType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WeaponType left, WeaponType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WeaponType left, WeaponType right)
    {
        return !left.Equals(right);
    }
}
