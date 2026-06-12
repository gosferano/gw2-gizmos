namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public sealed class PvpSeasonsIdClient : BaseClient, IPvpSeasonsIdClient
{
    private readonly string _seasonId;

    public PvpSeasonsIdClient(HttpClient httpClient, string seasonId)
        : base(httpClient)
    {
        _seasonId = seasonId;
        Leaderboards = new PvpSeasonsIdLeaderboardsClient(httpClient, seasonId);
    }

    protected override string UriPath => $"/v2/pvp/seasons/{_seasonId}";

    public IPvpSeasonsIdLeaderboardsClient Leaderboards { get; }
}
