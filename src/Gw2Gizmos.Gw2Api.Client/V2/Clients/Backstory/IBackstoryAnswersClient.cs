using Gw2Gizmos.Gw2Api.Contract.V2.Backstory;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Backstory;

public interface IBackstoryAnswersClient
    : IAllExpandableClient<BackstoryAnswer>,
        IBulkExpandableClient<BackstoryAnswer, string>,
        IPaginatedClient<BackstoryAnswer>;
