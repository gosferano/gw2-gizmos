using Gw2Gizmos.Gw2Api.Contract.V2.BuildStorage;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public interface IAccountBuildStorageClient
    : IAllExpandableClient<BuildStorageBuild>,
        IBulkExpandableClient<BuildStorageBuild, int>,
        IPaginatedClient<BuildStorageBuild>;
