namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public readonly struct GuildUpgradeType : IEquatable<GuildUpgradeType>
{
    public static readonly GuildUpgradeType AccumulatingCurrency = new GuildUpgradeType("AccumulatingCurrency");
    public static readonly GuildUpgradeType BankBag = new GuildUpgradeType("BankBag");
    public static readonly GuildUpgradeType Boost = new GuildUpgradeType("Boost");
    public static readonly GuildUpgradeType Claimable = new GuildUpgradeType("Claimable");
    public static readonly GuildUpgradeType Consumable = new GuildUpgradeType("Consumable");
    public static readonly GuildUpgradeType Decoration = new GuildUpgradeType("Decoration");
    public static readonly GuildUpgradeType GuildHall = new GuildUpgradeType("GuildHall");
    public static readonly GuildUpgradeType GuildHallExpedition = new GuildUpgradeType("GuildHallExpedition");
    public static readonly GuildUpgradeType Hub = new GuildUpgradeType("Hub");
    public static readonly GuildUpgradeType Queue = new GuildUpgradeType("Queue");
    public static readonly GuildUpgradeType Unlock = new GuildUpgradeType("Unlock");

    public string Value { get; }

    private GuildUpgradeType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator GuildUpgradeType(string value) => new(value);

    public static implicit operator string(GuildUpgradeType value) => value.Value;

    public bool Equals(GuildUpgradeType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is GuildUpgradeType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(GuildUpgradeType left, GuildUpgradeType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GuildUpgradeType left, GuildUpgradeType right)
    {
        return !left.Equals(right);
    }
}
