namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public sealed class PvpSeasonsIdLeaderboardsClient : BaseBlobClient<string[]>, IPvpSeasonsIdLeaderboardsClient
{
    private readonly string _seasonId;

    internal PvpSeasonsIdLeaderboardsClient(HttpClient httpClient, string seasonId)
        : base(httpClient)
    {
        _seasonId = seasonId;
    }

    protected override string UriPath => $"/v2/pvp/seasons/{_seasonId}/leaderboards";

    public IPvpSeasonsIdLeaderboardsIdClient this[string leaderboardId] =>
        new PvpSeasonsIdLeaderboardsIdClient(HttpClient, _seasonId, leaderboardId);
}
