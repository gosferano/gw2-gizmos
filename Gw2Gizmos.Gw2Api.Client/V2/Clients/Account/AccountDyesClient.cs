namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountDyesClient : BaseBlobClient<int[]>, IAccountDyesClient
{
    internal AccountDyesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "v2/account/dyes";
}
