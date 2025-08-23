namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public struct ConsumableUnlockType
{
    public static readonly ConsumableUnlockType BagSlot = new("BagSlot");
    public static readonly ConsumableUnlockType BankTab = new("BankTab");
    public static readonly ConsumableUnlockType BuildLibrarySlot = new("BuildLibrarySlot");
    public static readonly ConsumableUnlockType BuildLoadoutTab = new("BuildLoadoutTab");
    public static readonly ConsumableUnlockType Champion = new("Champion");
    public static readonly ConsumableUnlockType CollectibleCapacity = new("CollectibleCapacity");
    public static readonly ConsumableUnlockType Content = new("Content");
    public static readonly ConsumableUnlockType CraftingRecipe = new("CraftingRecipe");
    public static readonly ConsumableUnlockType Dye = new("Dye");
    public static readonly ConsumableUnlockType GearLoadoutTab = new("GearLoadoutTab");
    public static readonly ConsumableUnlockType GliderSkin = new("GliderSkin");
    public static readonly ConsumableUnlockType JadeBotSkin = new("JadeBotSkin");
    public static readonly ConsumableUnlockType Minipet = new("Minipet");
    public static readonly ConsumableUnlockType MountSkin = new("MountSkin");
    public static readonly ConsumableUnlockType Outfit = new("Outfit");
    public static readonly ConsumableUnlockType RandomUnlock = new("RandomUnlock");
    public static readonly ConsumableUnlockType SharedSlot = new("SharedSlot");

    public string Value { get; }

    private ConsumableUnlockType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ConsumableUnlockType(string value) => new(value);

    public bool Equals(ConsumableUnlockType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ConsumableUnlockType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ConsumableUnlockType left, ConsumableUnlockType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ConsumableUnlockType left, ConsumableUnlockType right)
    {
        return !left.Equals(right);
    }
}
