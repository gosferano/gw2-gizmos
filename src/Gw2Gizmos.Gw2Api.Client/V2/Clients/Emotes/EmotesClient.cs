using Gw2Gizmos.Gw2Api.Contract.V2.Emotes;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emotes;

public class EmotesClient : BaseBulkAllClient<Emote, string>, IEmotesClient
{
    internal EmotesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/emotes";
}
