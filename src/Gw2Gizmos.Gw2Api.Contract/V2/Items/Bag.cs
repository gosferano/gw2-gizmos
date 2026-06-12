namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class Bag : Item
{
    public BagDetails Details { get; set; } = null!;
}

public sealed class BagDetails
{
    public int Size { get; set; }
    public bool NoSellOrSort { get; set; }
}
