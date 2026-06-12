namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountGlidersClient : BaseBlobClient<int[]>, IAccountGlidersClient
{
    internal AccountGlidersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/gliders";
}
