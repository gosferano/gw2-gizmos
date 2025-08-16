using Gw2Gizmos.Gw2Api.Contract.Stories;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Stories;

public class StoriesSeasonsClient : BaseBulkAllClient<StorySeason, string>, IStoriesSeasonsClient
{
    internal StoriesSeasonsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/stories/seasons";
}
