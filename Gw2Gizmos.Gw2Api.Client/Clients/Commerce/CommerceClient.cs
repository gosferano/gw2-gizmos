namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceClient : ICommerceClient
{
    internal CommerceClient(IGw2ApiClient apiClient)
    {
        Delivery = new CommerceDeliveryClient(apiClient);
        Exchange = new CommerceExchangeClient(apiClient);
        Listings = new CommerceListingsClient(apiClient);
    }

    public ICommerceDeliveryClient Delivery { get; }
    public ICommerceExchangeClient Exchange { get; }
    public ICommerceListingsClient Listings { get; }
}
