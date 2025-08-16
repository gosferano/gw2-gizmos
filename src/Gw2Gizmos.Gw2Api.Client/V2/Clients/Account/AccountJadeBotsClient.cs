namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountJadeBotsClient : BaseBlobClient<int[]>, IAccountJadeBotsClient
{
    internal AccountJadeBotsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/jadebots";
}
