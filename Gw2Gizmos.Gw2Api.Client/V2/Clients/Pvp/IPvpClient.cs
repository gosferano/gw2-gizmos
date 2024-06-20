namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpClient : IBlobClient<string[]>
{
    IPvpAmuletsClient Amulets { get; }
    IPvpGamesClient Games { get; }
    IPvpHeroesClient Heroes { get; }
    IPvpRanksClient Ranks { get; }
}
