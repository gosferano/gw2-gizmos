namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsClient : BaseBlobClient<string[]>, ICommerceTransactionsClient
{
    internal CommerceTransactionsClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Current = new CommerceTransactionsCurrentClient(apiClient);
        History = new CommerceTransactionsHistoryClient(apiClient);
    }

    protected override string UriPath => "/v2/commerce/transactions";

    public ICommerceTransactionsCurrentClient Current { get; }
    public ICommerceTransactionsHistoryClient History { get; }
}
