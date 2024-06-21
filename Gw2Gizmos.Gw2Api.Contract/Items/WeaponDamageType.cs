namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct WeaponDamageType : IEquatable<WeaponDamageType>
{
    public static readonly WeaponDamageType Physical = new("Physical");
    public static readonly WeaponDamageType Fire = new("Fire");
    public static readonly WeaponDamageType Lightning = new("Lightning");
    public static readonly WeaponDamageType Ice = new("Ice");
    public static readonly WeaponDamageType Choking = new("Choking");
    public static readonly WeaponDamageType Ethereal = new("Ethereal");
    public static readonly WeaponDamageType Dark = new("Dark");
    public static readonly WeaponDamageType Divine = new("Divine");
    public static readonly WeaponDamageType Poison = new("Poison");
    public static readonly WeaponDamageType Bleeding = new("Bleeding");
    public static readonly WeaponDamageType Condition = new("Condition");
    public static readonly WeaponDamageType Magic = new("Magic");
    public static readonly WeaponDamageType Unknown = new("Unknown");

    public string Value { get; }

    private WeaponDamageType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WeaponDamageType(string value) => new(value);

    public static implicit operator string(WeaponDamageType value) => value.Value;

    public bool Equals(WeaponDamageType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WeaponDamageType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WeaponDamageType left, WeaponDamageType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WeaponDamageType left, WeaponDamageType right)
    {
        return !left.Equals(right);
    }
}
