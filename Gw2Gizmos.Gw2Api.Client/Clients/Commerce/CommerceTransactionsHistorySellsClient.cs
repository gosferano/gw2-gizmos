using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsHistorySellsClient
    : BasePaginatedBlobClient<CommerceTransaction>,
        ICommerceTransactionsHistorySellsClient
{
    internal CommerceTransactionsHistorySellsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/commerce/transactions/history/sells";
}
