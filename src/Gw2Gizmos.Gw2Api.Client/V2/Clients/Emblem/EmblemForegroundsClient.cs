namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emblem;

public class EmblemForegroundsClient : BaseBulkAllClient<Contract.V2.Emblem.Emblem, int>, IEmblemForegroundsClient
{
    internal EmblemForegroundsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/emblem/foregrounds";
}
