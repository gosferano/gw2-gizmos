namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class Gathering : Item
{
    public GatheringDetails Details { get; set; }
}

public class GatheringDetails
{
    public GatheringType Type { get; set; }
}
