using Gw2Gizmos.Gw2Api.Contract.Masteries;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Masteries;

public interface IMasteriesClient
    : IAllExpandableClient<Mastery>,
        IBulkExpandableClient<Mastery, int>,
        IPaginatedClient<Mastery>;
