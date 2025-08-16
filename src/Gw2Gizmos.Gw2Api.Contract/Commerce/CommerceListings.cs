namespace Gw2Gizmos.Gw2Api.Contract.Commerce;

public class CommerceListings
{
    public int Id { get; set; }
    public CommerceListing[] Buys { get; set; } = Array.Empty<CommerceListing>();
    public CommerceListing[] Sells { get; set; } = Array.Empty<CommerceListing>();
}
