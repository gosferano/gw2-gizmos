using Gw2Gizmos.Gw2Api.Contract.Quests;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Quests;

public interface IQuestsClient
    : IAllExpandableClient<Quest>,
        IBulkExpandableClient<Quest, int>,
        IPaginatedClient<Quest>;
