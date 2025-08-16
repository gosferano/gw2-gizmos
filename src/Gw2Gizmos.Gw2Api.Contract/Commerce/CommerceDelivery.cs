namespace Gw2Gizmos.Gw2Api.Contract.Commerce;

public class CommerceDelivery
{
    public int Coins { get; set; }
    public CommerceDeliveryItem[] Items { get; set; } = Array.Empty<CommerceDeliveryItem>();
}
