namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceExchangeClient : BaseBlobClient<string[]>, ICommerceExchangeClient
{
    internal CommerceExchangeClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/commerce/exchange";
}
