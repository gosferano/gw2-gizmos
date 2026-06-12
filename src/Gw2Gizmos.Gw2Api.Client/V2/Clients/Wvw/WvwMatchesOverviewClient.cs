using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwMatchesOverviewClient : BaseBulkAllClient<WvwMatchOverview, string>, IWvwMatchesOverviewClient
{
    internal WvwMatchesOverviewClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/matches/overview";

    public IWvwMatchesOverviewWorldClient WithWorldId(int worldId)
    {
        return new WvwMatchesOverviewWorldClient(HttpClient, worldId);
    }
}
