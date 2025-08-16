using Gw2Gizmos.Gw2Api.Contract.Masteries;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Masteries;

public interface IMasteriesClient
    : IAllExpandableClient<Mastery>,
        IBulkExpandableClient<Mastery, int>,
        IPaginatedClient<Mastery>;
