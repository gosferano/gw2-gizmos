namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct UpgradeComponentFlag : IEquatable<UpgradeComponentFlag>
{
    // Weapons
    public static readonly UpgradeComponentFlag Axe = new("Axe");
    public static readonly UpgradeComponentFlag Dagger = new("Dagger");
    public static readonly UpgradeComponentFlag Focus = new("Focus");
    public static readonly UpgradeComponentFlag Greatsword = new("Greatsword");
    public static readonly UpgradeComponentFlag Hammer = new("Hammer");
    public static readonly UpgradeComponentFlag Harpoon = new("Harpoon");
    public static readonly UpgradeComponentFlag LongBow = new("LongBow");
    public static readonly UpgradeComponentFlag Mace = new("Mace");
    public static readonly UpgradeComponentFlag Pistol = new("Pistol");
    public static readonly UpgradeComponentFlag Rifle = new("Rifle");
    public static readonly UpgradeComponentFlag Scepter = new("Scepter");
    public static readonly UpgradeComponentFlag Shield = new("Shield");
    public static readonly UpgradeComponentFlag ShortBow = new("ShortBow");
    public static readonly UpgradeComponentFlag Speargun = new("Speargun");
    public static readonly UpgradeComponentFlag Staff = new("Staff");
    public static readonly UpgradeComponentFlag Sword = new("Sword");
    public static readonly UpgradeComponentFlag Torch = new("Torch");
    public static readonly UpgradeComponentFlag Trident = new("Trident");
    public static readonly UpgradeComponentFlag Warhorn = new("Warhorn");

    // Armor
    public static readonly UpgradeComponentFlag HeavyArmor = new("HeavyArmor");
    public static readonly UpgradeComponentFlag MediumArmor = new("MediumArmor");
    public static readonly UpgradeComponentFlag LightArmor = new("LightArmor");

    // Trinkets
    public static readonly UpgradeComponentFlag Trinket = new("Trinket");

    public string Value { get; }

    private UpgradeComponentFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator UpgradeComponentFlag(string value) => new(value);

    public bool Equals(UpgradeComponentFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is UpgradeComponentFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(UpgradeComponentFlag left, UpgradeComponentFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UpgradeComponentFlag left, UpgradeComponentFlag right)
    {
        return !left.Equals(right);
    }
}
