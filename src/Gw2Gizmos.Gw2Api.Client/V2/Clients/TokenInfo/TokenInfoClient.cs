namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.TokenInfo;

public class TokenInfoClient : BaseBlobClient<Contract.V2.TokenInfo.TokenInfo>, ITokenInfoClient
{
    public TokenInfoClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/tokeninfo";
}
