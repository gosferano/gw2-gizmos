using Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public class PvpSeasonsIdLeaderboardsIdRegionIdClient
    : BasePaginatedBlobClient<PvpSeasonLeaderboardEntry>,
        IPvpSeasonsIdLeaderboardsIdRegionIdClient
{
    private readonly string _seasonId;
    private readonly string _leaderboardId;
    private readonly string _regionId;

    internal PvpSeasonsIdLeaderboardsIdRegionIdClient(
        HttpClient httpClient,
        string seasonId,
        string leaderboardId,
        string regionId
    )
        : base(httpClient)
    {
        _seasonId = seasonId;
        _leaderboardId = leaderboardId;
        _regionId = regionId;
    }

    protected override string UriPath => $"/v2/pvp/seasons/{_seasonId}/leaderboards/{_leaderboardId}/{_regionId}";
}
