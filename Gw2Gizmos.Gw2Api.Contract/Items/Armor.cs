namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Armor : Item {
    public ArmorDetails Details { get; set; }
}

public class ArmorDetails
{
    public ArmorSlotType Type { get; set; }
    public ArmorWeightClass WeightClass { get; set; }
    public int Defense { get; set; }
    public double AttributeAdjustment { get; set; }
     
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }
    public int[] StatChoices { get; set; } = Array.Empty<int>();
}