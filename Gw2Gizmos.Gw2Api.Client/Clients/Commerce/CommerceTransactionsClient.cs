namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsClient : BaseBlobClient<string[]>, ICommerceTransactionsClient
{
    internal CommerceTransactionsClient(HttpClient httpClient)
        : base(httpClient)
    {
        Current = new CommerceTransactionsCurrentClient(httpClient);
        History = new CommerceTransactionsHistoryClient(httpClient);
    }

    protected override string UriPath => "/v2/commerce/transactions";

    public ICommerceTransactionsCurrentClient Current { get; }
    public ICommerceTransactionsHistoryClient History { get; }
}
