namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpSeasonsIdLeaderboardsClient : IBlobClient<string[]>
{
    IPvpSeasonsIdLeaderboardsIdClient this[string leaderboardId] { get; }
}
