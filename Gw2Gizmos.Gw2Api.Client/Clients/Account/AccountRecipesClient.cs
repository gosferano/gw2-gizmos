namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountRecipesClient : BaseBlobClient<int[]>, IAccountRecipesClient
{
    internal AccountRecipesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/recipes";
}
