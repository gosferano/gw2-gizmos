namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public readonly struct GuildUpgradeCostType : IEquatable<GuildUpgradeCostType>
{
    public static readonly GuildUpgradeCostType Item = new("Item");
    public static readonly GuildUpgradeCostType Collectible = new("Collectible");
    public static readonly GuildUpgradeCostType Currency = new("Currency");
    public static readonly GuildUpgradeCostType Coins = new("Coins");

    public string Value { get; }

    private GuildUpgradeCostType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator GuildUpgradeCostType(string value) => new(value);

    public static implicit operator string(GuildUpgradeCostType value) => value.Value;

    public bool Equals(GuildUpgradeCostType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is GuildUpgradeCostType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(GuildUpgradeCostType left, GuildUpgradeCostType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GuildUpgradeCostType left, GuildUpgradeCostType right)
    {
        return !left.Equals(right);
    }
}
