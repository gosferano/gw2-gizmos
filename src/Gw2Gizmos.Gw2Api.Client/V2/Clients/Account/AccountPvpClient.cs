namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountPvpClient : BaseClient, IAccountPvpClient
{
    internal AccountPvpClient(HttpClient httpClient)
        : base(httpClient)
    {
        Heroes = new AccountPvpHeroesClient(httpClient);
    }

    protected override string UriPath => "/v2/account/pvp";

    public IAccountPvpHeroesClient Heroes { get; }
}
