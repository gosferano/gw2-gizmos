using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountWalletClient : BaseBlobClient<AccountWalletCurrency[]>, IAccountWalletClient
{
    internal AccountWalletClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/wallet";
}
