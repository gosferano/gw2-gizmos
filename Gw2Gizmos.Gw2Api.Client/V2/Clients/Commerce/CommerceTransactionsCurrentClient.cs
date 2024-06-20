namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public class CommerceTransactionsCurrentClient : BaseBlobClient<string[]>, ICommerceTransactionsCurrentClient
{
    internal CommerceTransactionsCurrentClient(HttpClient httpClient)
        : base(httpClient)
    {
        Buys = new CommerceTransactionsCurrentBuysClient(httpClient);
        Sells = new CommerceTransactionsCurrentSellsClient(httpClient);
    }

    protected override string UriPath => "/v2/commerce/transactions/current";

    public ICommerceTransactionsCurrentBuysClient Buys { get; }
    public ICommerceTransactionsCurrentSellsClient Sells { get; }
}
