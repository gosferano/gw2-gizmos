namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsHistoryClient : BaseBlobClient<string[]>, ICommerceTransactionsHistoryClient
{
    internal CommerceTransactionsHistoryClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Buys = new CommerceTransactionsHistoryBuysClient(apiClient);
        Sells = new CommerceTransactionsHistorySellsClient(apiClient);
    }

    protected override string UriPath => "/v2/commerce/transactions/history";

    public ICommerceTransactionsHistoryBuysClient Buys { get; }
    public ICommerceTransactionsHistorySellsClient Sells { get; }
}
