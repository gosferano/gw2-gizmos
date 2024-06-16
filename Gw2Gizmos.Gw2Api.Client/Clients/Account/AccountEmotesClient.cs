namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountEmotesClient : BaseBlobClient<string[]>, IAccountEmotesClient
{
    internal AccountEmotesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/emotes";
}
