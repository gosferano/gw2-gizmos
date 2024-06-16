using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountMaterialsClient : BaseBlobClient<AccountMaterial[]>, IAccountMaterialsClient
{
    internal AccountMaterialsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/materials";
}
