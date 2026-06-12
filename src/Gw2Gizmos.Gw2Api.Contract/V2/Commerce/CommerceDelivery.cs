namespace Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

public sealed class CommerceDelivery
{
    public int Coins { get; set; }
    public CommerceDeliveryItem[] Items { get; set; } = Array.Empty<CommerceDeliveryItem>();
}
