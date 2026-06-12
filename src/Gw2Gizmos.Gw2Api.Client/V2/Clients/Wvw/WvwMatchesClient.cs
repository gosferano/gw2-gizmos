using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwMatchesClient : BaseBulkAllClient<WvwMatch, string>, IWvwMatchesClient
{
    internal WvwMatchesClient(HttpClient httpClient)
        : base(httpClient)
    {
        Scores = new WvwMatchesScoresClient(httpClient);
        Stats = new WvwMatchesStatsClient(httpClient);
        Overview = new WvwMatchesOverviewClient(httpClient);
    }

    protected override string UriPath => "/v2/wvw/matches";

    public IWvwMatchesScoresClient Scores { get; }
    public IWvwMatchesStatsClient Stats { get; }
    public IWvwMatchesOverviewClient Overview { get; }
}
