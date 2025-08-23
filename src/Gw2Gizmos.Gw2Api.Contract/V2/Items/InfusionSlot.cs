namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class InfusionSlot
{
    public InfusionSlotFlag[] Flags { get; set; } = Array.Empty<InfusionSlotFlag>();
    public int? ItemId { get; set; }
}
