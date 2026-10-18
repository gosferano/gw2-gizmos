namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

public sealed class HomesteadClient : BaseBlobClient<string[]>, IHomesteadClient
{
    internal HomesteadClient(HttpClient httpClient)
        : base(httpClient)
    {
        Decorations = new HomesteadDecorationsClient(httpClient);
        Glyphs = new HomesteadGlyphsClient(httpClient);
    }

    protected override string UriPath => "/v2/homestead";

    public IHomesteadDecorationsClient Decorations { get; }
    public IHomesteadGlyphsClient Glyphs { get; }
}
