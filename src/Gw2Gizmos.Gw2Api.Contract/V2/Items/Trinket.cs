namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class Trinket : Item
{
    public TrinketDetails Details { get; set; } = null!;
}

public sealed class TrinketDetails
{
    public TrinketType Type { get; set; }
    public InfusionSlot[] InfusionSlots { get; set; } = Array.Empty<InfusionSlot>();
    public decimal AttributeAdjustment { get; set; }
    public InfixUpgrade? InfixUpgrade { get; set; }
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }
    public int[] StatChoices { get; set; } = Array.Empty<int>();
}
