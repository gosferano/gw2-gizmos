namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public class Bag : Item
{
    public BagDetails Details { get; set; }
}

public class BagDetails
{
    public int Size { get; set; }
    public bool NoSellOrSort { get; set; }
}
