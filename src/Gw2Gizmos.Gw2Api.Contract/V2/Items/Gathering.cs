namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class Gathering : Item
{
    public GatheringDetails Details { get; set; } = null!;
}

public sealed class GatheringDetails
{
    public GatheringType Type { get; set; }
}
