using Gw2Gizmos.Gw2Api.Contract.Legends;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Legends;

public class LegendsClient : BaseBulkAllClient<Legend, string>, ILegendsClient
{
    internal LegendsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/legends";
}
