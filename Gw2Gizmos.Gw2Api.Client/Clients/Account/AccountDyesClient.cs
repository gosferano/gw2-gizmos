namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountDyesClient : BaseBlobClient<int[]>, IAccountDyesClient
{
    internal AccountDyesClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "v2/account/dyes";
}
