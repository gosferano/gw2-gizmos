using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountBankClient : BaseBlobClient<AccountItem?[]>, IAccountBankClient
{
    internal AccountBankClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/account/bank";
}
