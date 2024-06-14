namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct ConsumableType : IEquatable<ConsumableType>
{
    public static readonly ConsumableType AppearanceChange = new("AppearanceChange");
    public static readonly ConsumableType Booze = new("Booze");
    public static readonly ConsumableType ContractNpc = new("ContractNpc");
    public static readonly ConsumableType Currency = new("Currency");
    public static readonly ConsumableType Food = new("Food");
    public static readonly ConsumableType Generic = new("Generic");
    public static readonly ConsumableType Halloween = new("Halloween");
    public static readonly ConsumableType Immediate = new("Immediate");
    public static readonly ConsumableType MountRandomUnlock = new("MountRandomUnlock");
    public static readonly ConsumableType RandomUnlock = new("RandomUnlock");
    public static readonly ConsumableType Transmutation = new("Transmutation");
    public static readonly ConsumableType Unlock = new("Unlock");
    public static readonly ConsumableType UpgradeRemoval = new("UpgradeRemoval");
    public static readonly ConsumableType Utility = new("Utility");
    public static readonly ConsumableType TeleportToFriend = new("TeleportToFriend");

    public string Value { get; }

    private ConsumableType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ConsumableType(string value) => new(value);

    public bool Equals(ConsumableType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ConsumableType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ConsumableType left, ConsumableType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ConsumableType left, ConsumableType right)
    {
        return !left.Equals(right);
    }
}
