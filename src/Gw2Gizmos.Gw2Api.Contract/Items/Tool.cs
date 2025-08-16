namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Tool : Item
{
    public ToolDetails Details { get; set; }
}

public class ToolDetails
{
    public ToolType Type { get; set; }
    public int Charges { get; set; }
}
