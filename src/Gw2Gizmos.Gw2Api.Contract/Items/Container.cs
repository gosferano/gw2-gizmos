namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Container : Item
{
    public ContainerDetails Details { get; set; }
}

public class ContainerDetails
{
    public ContainerType Type { get; set; }
}
