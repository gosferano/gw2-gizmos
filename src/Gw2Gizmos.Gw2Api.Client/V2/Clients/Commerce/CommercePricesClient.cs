using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public sealed class CommercePricesClient : BaseBulkClient<CommercePrices, int>, ICommercePricesClient
{
    internal CommercePricesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/commerce/prices";
}
