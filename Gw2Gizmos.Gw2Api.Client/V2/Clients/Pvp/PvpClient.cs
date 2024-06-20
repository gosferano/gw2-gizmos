namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public class PvpClient : BaseBlobClient<string[]>, IPvpClient
{
    internal PvpClient(HttpClient httpClient)
        : base(httpClient)
    {
        Amulets = new PvpAmuletsClient(httpClient);
        Games = new PvpGamesClient(httpClient);
        Heroes = new PvpHeroesClient(httpClient);
    }

    protected override string UriPath => "/v2/pvp";

    public IPvpAmuletsClient Amulets { get; }
    public IPvpGamesClient Games { get; }
    public IPvpHeroesClient Heroes { get; }
}
