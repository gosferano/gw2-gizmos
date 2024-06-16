namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountMinisClient : BaseBlobClient<int[]>, IAccountMinisClient
{
    internal AccountMinisClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/minis";
}
