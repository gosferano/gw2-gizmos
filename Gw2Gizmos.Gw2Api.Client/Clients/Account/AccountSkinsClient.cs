namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountSkinsClient : BaseBlobClient<int[]>, IAccountSkinsClient
{
    internal AccountSkinsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/skins";
}
