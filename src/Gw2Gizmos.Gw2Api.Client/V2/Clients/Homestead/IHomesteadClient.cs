namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

public interface IHomesteadClient : IBlobClient<string[]>
{
    public IHomesteadDecorationsClient Decorations { get; }
    public IHomesteadGlyphsClient Glyphs { get; }
}
