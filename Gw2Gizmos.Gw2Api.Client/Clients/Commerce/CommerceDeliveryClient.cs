using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceDeliveryClient : BaseBlobClient<CommerceDelivery>, ICommerceDeliveryClient
{
    internal CommerceDeliveryClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "v2/commerce/delivery";
}
