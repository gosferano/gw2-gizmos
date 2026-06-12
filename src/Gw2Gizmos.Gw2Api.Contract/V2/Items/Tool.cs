namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class Tool : Item
{
    public ToolDetails Details { get; set; } = null!;
}

public sealed class ToolDetails
{
    public ToolType Type { get; set; }
    public int Charges { get; set; }
}
