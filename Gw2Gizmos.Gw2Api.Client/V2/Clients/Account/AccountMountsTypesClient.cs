namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountMountsTypesClient : BaseBlobClient<string[]>, IAccountMountsTypesClient
{
    internal AccountMountsTypesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/mounts/types";
}
