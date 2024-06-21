namespace Gw2Gizmos.Gw2Api.Contract.Skins;

public readonly struct SkinFlag : IEquatable<SkinFlag>
{
    public static readonly SkinFlag ShowInWardrobe = new("ShowInWardrobe");
    public static readonly SkinFlag NoCost = new("NoCost");
    public static readonly SkinFlag HideIfLocked = new("HideIfLocked");
    public static readonly SkinFlag OverrideRarity = new("OverrideRarity");

    public string Value { get; }

    private SkinFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator SkinFlag(string value) => new(value);

    public static implicit operator string(SkinFlag value) => value.Value;

    public bool Equals(SkinFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkinFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(SkinFlag left, SkinFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkinFlag left, SkinFlag right)
    {
        return !left.Equals(right);
    }
}
