namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountWorldBossesClient : BaseBlobClient<string[]>, IAccountWorldBossesClient
{
    internal AccountWorldBossesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/worldbosses";
}
