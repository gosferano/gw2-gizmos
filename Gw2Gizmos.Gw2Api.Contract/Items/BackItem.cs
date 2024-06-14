namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class BackItem : Item
{
    public BackItemDetails Details { get; set; }
}

public class BackItemDetails
{
    public InfusionSlot[] InfusionSlots { get; set; } = Array.Empty<InfusionSlot>();
    public decimal AttributeAdjustment { get; set; }
    public InfixUpgrade? InfixUpgrade { get; set; }
    public int? SuffixItemId { get; set; }
    public string SecondarySuffixItemId { get; set; } = "";
    public int[] StatChoices { get; set; } = Array.Empty<int>();
}