namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class Container : Item
{
    public ContainerDetails Details { get; set; } = null!;
}

public class ContainerDetails
{
    public ContainerType Type { get; set; }
}
