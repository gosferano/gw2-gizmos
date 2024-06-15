namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceExchangeCoinsClient : BaseClient, ICommerceExchangeCoinsClient
{
    internal CommerceExchangeCoinsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    public ICommerceExchangeCoinsQuantityClient Quantity(int quantity)
    {
        return new CommerceExchangeCoinsQuantityClient(ApiClient, quantity);
    }

    protected override string UriPath => "/v2/commerce/exchange/coins";
}
