using Gw2Gizmos.Gw2Api.Contract.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Backstory;

public interface IBackstoryAnswersClient
    : IAllExpandableClient<BackstoryAnswer>,
        IBulkExpandableClient<BackstoryAnswer, string>,
        IPaginatedClient<BackstoryAnswer>;
