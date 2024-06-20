using Gw2Gizmos.Gw2Api.Contract.JadeBots;

namespace Gw2Gizmos.Gw2Api.Client.Clients.JadeBots;

public class JadeBotsClient : BaseBulkAllClient<JadeBot, int>, IJadeBotsClient
{
    internal JadeBotsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/jadebots";
}
