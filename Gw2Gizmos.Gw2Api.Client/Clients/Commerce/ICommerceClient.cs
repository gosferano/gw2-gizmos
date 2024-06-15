namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommerceClient
{
    ICommerceDeliveryClient Delivery { get; }
    ICommerceExchangeClient Exchange { get; }
    ICommerceListingsClient Listings { get; }
}
