using Gw2Gizmos.Gw2Api.Contract.V2.JadeBots;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.JadeBots;

public sealed class JadeBotsClient : BaseBulkAllClient<JadeBot, int>, IJadeBotsClient
{
    internal JadeBotsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/jadebots";
}
