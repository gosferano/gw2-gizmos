namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class BackItem : Item
{
    public BackItemDetails Details { get; set; } = null!;
}

public class BackItemDetails
{
    public InfusionSlot[] InfusionSlots { get; set; } = Array.Empty<InfusionSlot>();
    public decimal AttributeAdjustment { get; set; }
    public InfixUpgrade? InfixUpgrade { get; set; }
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }
    public int[] StatChoices { get; set; } = Array.Empty<int>();
}
