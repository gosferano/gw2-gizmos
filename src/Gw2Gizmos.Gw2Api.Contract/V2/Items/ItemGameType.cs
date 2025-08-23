namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct ItemGameType : IEquatable<ItemGameType>
{
    public static readonly ItemGameType Activity = new("Activity");
    public static readonly ItemGameType Dungeon = new("Dungeon");
    public static readonly ItemGameType Pve = new("Pve");
    public static readonly ItemGameType Pvp = new("Pvp");
    public static readonly ItemGameType PvpLobby = new("PvpLobby");
    public static readonly ItemGameType Wvw = new("Wvw");

    public string Value { get; }

    private ItemGameType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ItemGameType(string value) => new(value);

    public bool Equals(ItemGameType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemGameType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ItemGameType left, ItemGameType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemGameType left, ItemGameType right)
    {
        return !left.Equals(right);
    }
}
