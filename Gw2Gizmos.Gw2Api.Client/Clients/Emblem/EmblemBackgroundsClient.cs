namespace Gw2Gizmos.Gw2Api.Client.Clients.Emblem;

public class EmblemBackgroundsClient : BaseBulkAllClient<Contract.Emblem.Emblem, int>, IEmblemBackgroundsClient
{
    internal EmblemBackgroundsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/emblem/backgrounds";
}
