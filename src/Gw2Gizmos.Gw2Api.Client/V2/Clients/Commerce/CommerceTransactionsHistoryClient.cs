namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public sealed class CommerceTransactionsHistoryClient : BaseBlobClient<string[]>, ICommerceTransactionsHistoryClient
{
    internal CommerceTransactionsHistoryClient(HttpClient httpClient)
        : base(httpClient)
    {
        Buys = new CommerceTransactionsHistoryBuysClient(httpClient);
        Sells = new CommerceTransactionsHistorySellsClient(httpClient);
    }

    protected override string UriPath => "/v2/commerce/transactions/history";

    public ICommerceTransactionsHistoryBuysClient Buys { get; }
    public ICommerceTransactionsHistorySellsClient Sells { get; }
}
