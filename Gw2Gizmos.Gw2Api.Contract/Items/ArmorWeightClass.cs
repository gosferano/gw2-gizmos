namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ArmorWeightClass
{
    public static ArmorWeightClass Heavy = new(ArmorWeightClasses.Heavy);
    public static ArmorWeightClass Medium = new(ArmorWeightClasses.Medium);
    public static ArmorWeightClass Light = new(ArmorWeightClasses.Light);
    public static ArmorWeightClass Clothing = new(ArmorWeightClasses.Clothing);

    public string Value { get; }

    private ArmorWeightClass(string value)
    {
        Value = value;
    }

    public static implicit operator ArmorWeightClass(string value) => new(value);
}