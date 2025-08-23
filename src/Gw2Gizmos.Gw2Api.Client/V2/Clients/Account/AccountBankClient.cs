using Gw2Gizmos.Gw2Api.Contract.V2.Account;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountBankClient : BaseBlobClient<AccountItem?[]>, IAccountBankClient
{
    internal AccountBankClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/bank";
}
