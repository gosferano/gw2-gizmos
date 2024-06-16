using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceListingsClient : BaseBulkClient<CommerceListings, int>, ICommerceListingsClient
{
    internal CommerceListingsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/commerce/listings";
}
