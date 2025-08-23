using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwMatchesScoresClient : BaseBulkAllClient<WvwMatchScores, string>, IWvwMatchesScoresClient
{
    internal WvwMatchesScoresClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/matches/scores";

    public IWvwMatchesScoresWorldClient WithWorldId(int worldId)
    {
        return new WvwMatchesScoresWorldClient(HttpClient, worldId);
    }
}
