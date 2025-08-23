using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public class CommerceTransactionsHistorySellsClient
    : BasePaginatedBlobClient<CommerceTransaction>,
        ICommerceTransactionsHistorySellsClient
{
    internal CommerceTransactionsHistorySellsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/commerce/transactions/history/sells";
}
