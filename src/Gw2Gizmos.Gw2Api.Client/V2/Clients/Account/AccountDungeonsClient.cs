namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountDungeonsClient : BaseBlobClient<string[]>, IAccountDungeonsClient
{
    internal AccountDungeonsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/dungeons";
}
