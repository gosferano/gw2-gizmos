using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwMatchesStatsClient : BaseBulkAllClient<WvwMatchStats, string>, IWvwMatchesStatsClient
{
    internal WvwMatchesStatsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/matches/stats";

    public IWvwMatchesStatsWorldClient WithWorldId(int worldId)
    {
        return new WvwMatchesStatsWorldClient(HttpClient, worldId);
    }
}
