namespace Gw2Gizmos.Gw2Api.Contract.V2.Skins;

public readonly struct SkinType : IEquatable<SkinType>
{
    public static readonly SkinType Armor = new("Armor");
    public static readonly SkinType Back = new("Back");
    public static readonly SkinType Gathering = new("Gathering");
    public static readonly SkinType Weapon = new("Weapon");

    public string Value { get; }

    private SkinType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkinType(string value) => new(value);

    public static implicit operator string(SkinType value) => value.Value;

    public bool Equals(SkinType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkinType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkinType left, SkinType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkinType left, SkinType right)
    {
        return !left.Equals(right);
    }
}
