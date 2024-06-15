namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceExchangeGemsClient : BaseClient, ICommerceExchangeGemsClient
{
    internal CommerceExchangeGemsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/commerce/exchange/gems";

    public ICommerceExchangeGemsQuantityClient Quantity(int quantity)
    {
        return new CommerceExchangeGemsQuantityClient(ApiClient, quantity);
    }
}
