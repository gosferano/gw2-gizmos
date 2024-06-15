namespace Gw2Gizmos.Gw2Api.Contract.Commerce;

public class CommercePrices
{
    public int Id { get; set; }
    public bool Whitelisted { get; set; }
    public CommercePrice Buys { get; set; }
    public CommercePrice Sells { get; set; }
}
