namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public readonly struct CharacterEquipmentLocation : IEquatable<CharacterEquipmentLocation>
{
    public static readonly CharacterEquipmentLocation Equipped = new CharacterEquipmentLocation("Equipped");
    public static readonly CharacterEquipmentLocation Armory = new CharacterEquipmentLocation("Armory");
    public static readonly CharacterEquipmentLocation EquippedFromLegendaryArmory = new CharacterEquipmentLocation(
        "EquippedFromLegendaryArmory"
    );
    public static readonly CharacterEquipmentLocation LegendaryArmory = new CharacterEquipmentLocation(
        "LegendaryArmory "
    );

    public string Value { get; }

    private CharacterEquipmentLocation(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator CharacterEquipmentLocation(string value) => new(value);

    public static implicit operator string(CharacterEquipmentLocation value) => value.Value;

    public bool Equals(CharacterEquipmentLocation other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CharacterEquipmentLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CharacterEquipmentLocation left, CharacterEquipmentLocation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CharacterEquipmentLocation left, CharacterEquipmentLocation right)
    {
        return !left.Equals(right);
    }
}
