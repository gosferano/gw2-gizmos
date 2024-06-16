namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountDungeonsClient : BaseBlobClient<string[]>, IAccountDungeonsClient
{
    internal AccountDungeonsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/dungeons";
}
