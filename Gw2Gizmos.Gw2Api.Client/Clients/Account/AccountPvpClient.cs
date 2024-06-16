namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountPvpClient : BaseClient, IAccountPvpClient
{
    internal AccountPvpClient(HttpClient httpClient)
        : base(httpClient)
    {
        Heroes = new AccountPvpHeroesClient(httpClient);
    }

    protected override string UriPath => "/v2/account/pvp";

    public IAccountPvpHeroesClient Heroes { get; }
}
