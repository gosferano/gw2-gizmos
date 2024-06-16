namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountNoveltiesClient : BaseBlobClient<int[]>, IAccountNoveltiesClient
{
    internal AccountNoveltiesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/novelties";
}
