using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountLegendaryArmoryClient : BaseBlobClient<AccountLegendaryArmoryItem[]>, IAccountLegendaryArmoryClient
{
    internal AccountLegendaryArmoryClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/legendaryarmory";
}
