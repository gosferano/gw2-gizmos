using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public class CommerceTransactionsCurrentBuysClient
    : BasePaginatedBlobClient<CommerceTransaction>,
        ICommerceTransactionsCurrentBuysClient
{
    internal CommerceTransactionsCurrentBuysClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/commerce/transactions/current/buys";
}
