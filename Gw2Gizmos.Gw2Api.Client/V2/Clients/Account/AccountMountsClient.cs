namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountMountsClient : BaseBlobClient<string[]>, IAccountMountsClient
{
    internal AccountMountsClient(HttpClient httpClient)
        : base(httpClient)
    {
        Skins = new AccountMountsSkinsClient(httpClient);
        Types = new AccountMountsTypesClient(httpClient);
    }

    protected override string UriPath => "/v2/account/mounts";

    public IAccountMountsSkinsClient Skins { get; }
    public IAccountMountsTypesClient Types { get; }
}
