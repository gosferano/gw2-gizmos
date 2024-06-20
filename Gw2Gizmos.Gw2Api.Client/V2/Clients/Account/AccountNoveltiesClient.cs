namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountNoveltiesClient : BaseBlobClient<int[]>, IAccountNoveltiesClient
{
    internal AccountNoveltiesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/novelties";
}
