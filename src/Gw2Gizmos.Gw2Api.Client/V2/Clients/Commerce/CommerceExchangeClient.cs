namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public sealed class CommerceExchangeClient : BaseBlobClient<string[]>, ICommerceExchangeClient
{
    internal CommerceExchangeClient(HttpClient httpClient)
        : base(httpClient)
    {
        Coins = new CommerceExchangeCoinsClient(httpClient);
        Gems = new CommerceExchangeGemsClient(httpClient);
    }

    protected override string UriPath => "/v2/commerce/exchange";

    public ICommerceExchangeCoinsClient Coins { get; }
    public ICommerceExchangeGemsClient Gems { get; }
}
