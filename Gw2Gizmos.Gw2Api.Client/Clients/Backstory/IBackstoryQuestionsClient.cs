using Gw2Gizmos.Gw2Api.Contract.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Backstory;

public interface IBackstoryQuestionsClient
    : IAllExpandableClient<BackstoryQuestion>,
        IBulkExpandableClient<BackstoryQuestion, int>,
        IPaginatedClient<BackstoryQuestion>;
