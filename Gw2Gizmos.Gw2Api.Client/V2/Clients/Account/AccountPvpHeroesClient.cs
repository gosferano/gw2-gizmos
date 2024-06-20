namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountPvpHeroesClient : BaseBlobClient<int[]>, IAccountPvpHeroesClient
{
    internal AccountPvpHeroesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/pvp/heroes";
}
