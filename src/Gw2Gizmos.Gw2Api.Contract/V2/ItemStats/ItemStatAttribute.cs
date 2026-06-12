namespace Gw2Gizmos.Gw2Api.Contract.V2.ItemStats;

public sealed class ItemStatAttribute
{
    public AttributeName Attribute { get; set; }
    public decimal Multiplier { get; set; }
    public int Value { get; set; }
}
