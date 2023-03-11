namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ArmorSlotType
{
    public static ArmorSlotType HelmAquatic = new(ArmorSlotTypes.HelmAquatic);
    public static ArmorSlotType Backpack = new(ArmorSlotTypes.Backpack);
    public static ArmorSlotType Coat = new(ArmorSlotTypes.Coat);
    public static ArmorSlotType Boots = new(ArmorSlotTypes.Boots);
    public static ArmorSlotType Gloves = new(ArmorSlotTypes.Gloves);
    public static ArmorSlotType Helm = new(ArmorSlotTypes.Helm);
    public static ArmorSlotType Leggings = new(ArmorSlotTypes.Leggings);
    public static ArmorSlotType Shoulders = new(ArmorSlotTypes.Shoulders);

    public string Value { get; }

    private ArmorSlotType(string value)
    {
        Value = value;
    }

    public static implicit operator ArmorSlotType(string value) => new(value);
}