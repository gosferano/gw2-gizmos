using Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

public sealed class HomesteadGlyphsClient : BaseBulkAllClient<HomesteadGlyph, string>, IHomesteadGlyphsClient
{
    internal HomesteadGlyphsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/homestead/glyphs";
}
