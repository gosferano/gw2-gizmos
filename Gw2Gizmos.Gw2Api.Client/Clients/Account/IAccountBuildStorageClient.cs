using Gw2Gizmos.Gw2Api.Contract.BuildStorage;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountBuildStorageClient
    : IAllExpandableClient<BuildStorageBuild>,
        IBulkExpandableClient<BuildStorageBuild, int>,
        IPaginatedClient<BuildStorageBuild>;
