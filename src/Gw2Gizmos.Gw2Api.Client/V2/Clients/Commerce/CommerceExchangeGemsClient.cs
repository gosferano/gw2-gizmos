namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public sealed class CommerceExchangeGemsClient : BaseClient, ICommerceExchangeGemsClient
{
    internal CommerceExchangeGemsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/commerce/exchange/gems";

    public ICommerceExchangeGemsQuantityClient Quantity(int quantity)
    {
        return new CommerceExchangeGemsQuantityClient(HttpClient, quantity);
    }
}
