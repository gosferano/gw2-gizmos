using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceTransactionsCurrentSellsClient
    : BasePaginatedBlobClient<CommerceTransaction>,
        ICommerceTransactionsCurrentSellsClient
{
    internal CommerceTransactionsCurrentSellsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/commerce/transactions/current/sells";
}
