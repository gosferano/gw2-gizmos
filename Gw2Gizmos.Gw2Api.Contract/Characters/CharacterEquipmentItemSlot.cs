namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public readonly struct CharacterEquipmentItemSlot : IEquatable<CharacterEquipmentItemSlot>
{
    public static readonly CharacterEquipmentItemSlot HelmAquatic = new CharacterEquipmentItemSlot("HelmAquatic");
    public static readonly CharacterEquipmentItemSlot Backpack = new CharacterEquipmentItemSlot("Backpack");
    public static readonly CharacterEquipmentItemSlot Coat = new CharacterEquipmentItemSlot("Coat");
    public static readonly CharacterEquipmentItemSlot Boots = new CharacterEquipmentItemSlot("Boots");
    public static readonly CharacterEquipmentItemSlot Gloves = new CharacterEquipmentItemSlot("Gloves");
    public static readonly CharacterEquipmentItemSlot Helm = new CharacterEquipmentItemSlot("Helm");
    public static readonly CharacterEquipmentItemSlot Leggings = new CharacterEquipmentItemSlot("Leggings");
    public static readonly CharacterEquipmentItemSlot Shoulders = new CharacterEquipmentItemSlot("Shoulders");
    public static readonly CharacterEquipmentItemSlot Accessory1 = new CharacterEquipmentItemSlot("Accessory1");
    public static readonly CharacterEquipmentItemSlot Accessory2 = new CharacterEquipmentItemSlot("Accessory2");
    public static readonly CharacterEquipmentItemSlot Ring1 = new CharacterEquipmentItemSlot("Ring1");
    public static readonly CharacterEquipmentItemSlot Ring2 = new CharacterEquipmentItemSlot("Ring2");
    public static readonly CharacterEquipmentItemSlot Amulet = new CharacterEquipmentItemSlot("Amulet");
    public static readonly CharacterEquipmentItemSlot WeaponAquaticA = new CharacterEquipmentItemSlot("WeaponAquaticA");
    public static readonly CharacterEquipmentItemSlot WeaponAquaticB = new CharacterEquipmentItemSlot("WeaponAquaticB");
    public static readonly CharacterEquipmentItemSlot WeaponA1 = new CharacterEquipmentItemSlot("WeaponA1");
    public static readonly CharacterEquipmentItemSlot WeaponA2 = new CharacterEquipmentItemSlot("WeaponA2");
    public static readonly CharacterEquipmentItemSlot WeaponB1 = new CharacterEquipmentItemSlot("WeaponB1");
    public static readonly CharacterEquipmentItemSlot WeaponB2 = new CharacterEquipmentItemSlot("WeaponB2");
    public static readonly CharacterEquipmentItemSlot Sickle = new CharacterEquipmentItemSlot("Sickle");
    public static readonly CharacterEquipmentItemSlot Axe = new CharacterEquipmentItemSlot("Axe");
    public static readonly CharacterEquipmentItemSlot Pick = new CharacterEquipmentItemSlot("Pick");

    public string Value { get; }

    private CharacterEquipmentItemSlot(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator CharacterEquipmentItemSlot(string value) => new(value);

    public static implicit operator string(CharacterEquipmentItemSlot value) => value.Value;

    public bool Equals(CharacterEquipmentItemSlot other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CharacterEquipmentItemSlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CharacterEquipmentItemSlot left, CharacterEquipmentItemSlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CharacterEquipmentItemSlot left, CharacterEquipmentItemSlot right)
    {
        return !left.Equals(right);
    }
}
