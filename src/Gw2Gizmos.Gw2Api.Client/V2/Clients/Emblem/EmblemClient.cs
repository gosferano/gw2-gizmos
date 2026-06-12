namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emblem;

public sealed class EmblemClient : BaseBlobClient<string[]>, IEmblemClient
{
    internal EmblemClient(HttpClient httpClient)
        : base(httpClient)
    {
        Backgrounds = new EmblemBackgroundsClient(httpClient);
        Foregrounds = new EmblemForegroundsClient(httpClient);
    }

    protected override string UriPath => "/v2/emblem";

    public IEmblemBackgroundsClient Backgrounds { get; }
    public IEmblemForegroundsClient Foregrounds { get; }
}
