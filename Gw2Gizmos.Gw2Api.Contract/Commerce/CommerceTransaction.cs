namespace Gw2Gizmos.Gw2Api.Contract.Commerce;

public class CommerceTransaction
{
    public long Id { get; set; }
    public int ItemId { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Purchased { get; set; }
}
