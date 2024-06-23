using Gw2Gizmos.Gw2Api.Contract.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwObjectivesClient
    : IAllExpandableClient<WvwObjective>,
        IBulkExpandableClient<WvwObjective, string>,
        IPaginatedClient<WvwObjective>;
