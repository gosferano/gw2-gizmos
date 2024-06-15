using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsHistoryBuysClient
    : BasePaginatedBlobClient<CommerceTransaction>,
        ICommerceTransactionsHistoryBuysClient
{
    internal CommerceTransactionsHistoryBuysClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/commerce/transactions/history/buys";
}
