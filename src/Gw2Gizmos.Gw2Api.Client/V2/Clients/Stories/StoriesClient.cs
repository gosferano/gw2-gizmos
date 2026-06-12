using Gw2Gizmos.Gw2Api.Contract.V2.Stories;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Stories;

public sealed class StoriesClient : BaseBulkAllClient<Story, int>, IStoriesClient
{
    internal StoriesClient(HttpClient httpClient)
        : base(httpClient)
    {
        Seasons = new StoriesSeasonsClient(httpClient);
    }

    protected override string UriPath => "/v2/stories";

    public IStoriesSeasonsClient Seasons { get; }
}
