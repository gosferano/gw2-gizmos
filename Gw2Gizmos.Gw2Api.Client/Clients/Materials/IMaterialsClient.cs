using Gw2Gizmos.Gw2Api.Contract.Materials;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Materials;

public interface IMaterialsClient
    : IAllExpandableClient<MaterialCategory>,
        IBulkExpandableClient<MaterialCategory, int>,
        IPaginatedClient<MaterialCategory> { }
