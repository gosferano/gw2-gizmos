namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpSeasonsIdLeaderboardsIdClient : IBlobClient<string[]>
{
    public IPvpSeasonsIdLeaderboardsIdRegionIdClient this[string regionId] { get; }
}
