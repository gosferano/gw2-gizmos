using Gw2Gizmos.Gw2Api.Contract.V2.Quests;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Quests;

public class QuestsClient : BaseBulkAllClient<Quest, int>, IQuestsClient
{
    internal QuestsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/quests";
}
