using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommercePricesClient : BaseBulkClient<CommercePrices, int>, ICommercePricesClient
{
    internal CommercePricesClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/commerce/prices";
}
