namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class Container : Item
{
    public ContainerDetails Details { get; set; } = null!;
}

public sealed class ContainerDetails
{
    public ContainerType Type { get; set; }
}
