namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountHomesteadClient : BaseBlobClient<string[]>, IAccountHomesteadClient
{
    internal AccountHomesteadClient(HttpClient httpClient)
        : base(httpClient)
    {
        Decorations = new AccountHomesteadDecorationsClient(httpClient);
        Glyphs = new AccountHomesteadGlyphsClient(httpClient);
    }

    protected override string UriPath => "/v2/account/homestead";

    public IAccountHomesteadDecorationsClient Decorations { get; }
    public IAccountHomesteadGlyphsClient Glyphs { get; }
}
