namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public interface IAccountHomesteadClient : IBlobClient<string[]>
{
    public IAccountHomesteadDecorationsClient Decorations { get; }
    public IAccountHomesteadGlyphsClient Glyphs { get; }
}
