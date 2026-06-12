namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public sealed class CommerceExchangeCoinsClient : BaseClient, ICommerceExchangeCoinsClient
{
    internal CommerceExchangeCoinsClient(HttpClient httpClient)
        : base(httpClient) { }

    public ICommerceExchangeCoinsQuantityClient Quantity(int quantity)
    {
        return new CommerceExchangeCoinsQuantityClient(HttpClient, quantity);
    }

    protected override string UriPath => "/v2/commerce/exchange/coins";
}
