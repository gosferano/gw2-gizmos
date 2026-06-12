namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountMountsSkinsClient : BaseBlobClient<int[]>, IAccountMountsSkinsClient
{
    internal AccountMountsSkinsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/mounts/skins";
}
