namespace Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

public class CommerceDelivery
{
    public int Coins { get; set; }
    public CommerceDeliveryItem[] Items { get; set; } = Array.Empty<CommerceDeliveryItem>();
}
