namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceExchangeClient : BaseBlobClient<string[]>, ICommerceExchangeClient
{
    internal CommerceExchangeClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Coins = new CommerceExchangeCoinsClient(apiClient);
        Gems = new CommerceExchangeGemsClient(apiClient);
    }

    protected override string UriPath => "/v2/commerce/exchange";

    public ICommerceExchangeCoinsClient Coins { get; }
    public ICommerceExchangeGemsClient Gems { get; }
}
