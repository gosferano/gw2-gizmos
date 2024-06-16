namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountMapChestsClient : BaseBlobClient<string[]>, IAccountMapChestsClient
{
    internal AccountMapChestsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/mapchests";
}
