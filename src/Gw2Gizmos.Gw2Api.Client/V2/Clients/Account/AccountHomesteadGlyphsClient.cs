namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountHomesteadGlyphsClient : BaseBlobClient<string[]>, IAccountHomesteadGlyphsClient
{
    internal AccountHomesteadGlyphsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/homestead/glyphs";
}
