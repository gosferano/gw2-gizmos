namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceClient : ICommerceClient
{
    internal CommerceClient(IGw2ApiClient apiClient)
    {
        Delivery = new CommerceDeliveryClient(apiClient);
    }

    public ICommerceDeliveryClient Delivery { get; }
}
