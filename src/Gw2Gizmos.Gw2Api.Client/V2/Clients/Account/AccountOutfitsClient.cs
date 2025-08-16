namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountOutfitsClient : BaseBlobClient<int[]>, IAccountOutfitsClient
{
    internal AccountOutfitsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/outfits";
}
