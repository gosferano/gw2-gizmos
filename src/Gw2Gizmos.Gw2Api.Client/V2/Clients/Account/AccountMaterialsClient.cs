using Gw2Gizmos.Gw2Api.Contract.V2.Account;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountMaterialsClient : BaseBlobClient<AccountMaterial[]>, IAccountMaterialsClient
{
    internal AccountMaterialsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/materials";
}
