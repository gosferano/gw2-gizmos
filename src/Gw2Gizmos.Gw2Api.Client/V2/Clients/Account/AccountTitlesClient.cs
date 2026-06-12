namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountTitlesClient : BaseBlobClient<int[]>, IAccountTitlesClient
{
    internal AccountTitlesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/titles";
}
