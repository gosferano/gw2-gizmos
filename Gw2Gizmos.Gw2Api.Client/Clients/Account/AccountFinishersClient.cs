using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountFinishersClient : BaseBlobClient<AccountFinisher[]>, IAccountFinishersClient
{
    internal AccountFinishersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/finishers";
}
