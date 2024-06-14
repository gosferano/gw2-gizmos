namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Trinket : Item
{
    public TrinketDetails Details { get; set; }
}

public class TrinketDetails
{
    public TrinketType Type { get; set; }
    public InfusionSlot[] InfusionSlots { get; set; } = Array.Empty<InfusionSlot>();
    public decimal AttributeAdjustment { get; set; }
    public InfixUpgrade InfixUpgrade { get; set; }
    public int? SuffixItemId { get; set; }
    public string SecondarySuffixItemId { get; set; } = "";
    public int[] StatChoices { get; set; } = Array.Empty<int>();
}