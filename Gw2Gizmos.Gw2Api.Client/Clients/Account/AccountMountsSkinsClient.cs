namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountMountsSkinsClient : BaseBlobClient<int[]>, IAccountMountsSkinsClient
{
    internal AccountMountsSkinsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/mounts/skins";
}
