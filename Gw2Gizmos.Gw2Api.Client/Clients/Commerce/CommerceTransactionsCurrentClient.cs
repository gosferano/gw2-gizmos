namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsCurrentClient : BaseBlobClient<string[]>, ICommerceTransactionsCurrentClient
{
    internal CommerceTransactionsCurrentClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Buys = new CommerceTransactionsCurrentBuysClient(apiClient);
        Sells = new CommerceTransactionsCurrentSellsClient(apiClient);
    }

    protected override string UriPath => "/v2/commerce/transactions/current";

    public ICommerceTransactionsCurrentBuysClient Buys { get; }
    public ICommerceTransactionsCurrentSellsClient Sells { get; }
}
