namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public sealed class PvpSeasonsIdLeaderboardsIdClient : BaseBlobClient<string[]>, IPvpSeasonsIdLeaderboardsIdClient
{
    private readonly string _seasonId;
    private readonly string _leaderboardId;

    internal PvpSeasonsIdLeaderboardsIdClient(HttpClient httpClient, string seasonId, string leaderboardId)
        : base(httpClient)
    {
        _seasonId = seasonId;
        _leaderboardId = leaderboardId;
    }

    protected override string UriPath => $"/v2/pvp/seasons/{_seasonId}/leaderboards/{_leaderboardId}";

    public IPvpSeasonsIdLeaderboardsIdRegionIdClient this[string regionId] =>
        new PvpSeasonsIdLeaderboardsIdRegionIdClient(HttpClient, _seasonId, _leaderboardId, regionId);
}
