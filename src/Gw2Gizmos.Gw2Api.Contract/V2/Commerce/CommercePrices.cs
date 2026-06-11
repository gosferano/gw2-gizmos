namespace Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

public class CommercePrices
{
    public int Id { get; set; }
    public bool Whitelisted { get; set; }
    public CommercePrice Buys { get; set; } = null!;
    public CommercePrice Sells { get; set; } = null!;
}
