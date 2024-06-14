namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct ItemRestriction : IEquatable<ItemRestriction>
{
    public static readonly ItemRestriction Asura = new("Asura");
    public static readonly ItemRestriction Charr = new("Charr");
    public static readonly ItemRestriction Human = new("Human");
    public static readonly ItemRestriction Norn = new("Norn");
    public static readonly ItemRestriction Sylvari = new("Sylvari");
    public static readonly ItemRestriction Elementalist = new("Elementalist");
    public static readonly ItemRestriction Engineer = new("Engineer");
    public static readonly ItemRestriction Guardian = new("Guardian");
    public static readonly ItemRestriction Mesmer = new("Mesmer");
    public static readonly ItemRestriction Necromancer = new("Necromancer");
    public static readonly ItemRestriction Ranger = new("Ranger");
    public static readonly ItemRestriction Revenant = new("Revenant");
    public static readonly ItemRestriction Thief = new("Thief");
    public static readonly ItemRestriction Warrior = new("Warrior");
    public static readonly ItemRestriction Female = new("Female");

    public string Value { get; }

    private ItemRestriction(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ItemRestriction(string value) => new(value);

    public bool Equals(ItemRestriction other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemRestriction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ItemRestriction left, ItemRestriction right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemRestriction left, ItemRestriction right)
    {
        return !left.Equals(right);
    }
}
