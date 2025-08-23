using Gw2Gizmos.Gw2Api.Contract.V2.Materials;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Materials;

public interface IMaterialsClient
    : IAllExpandableClient<MaterialCategory>,
        IBulkExpandableClient<MaterialCategory, int>,
        IPaginatedClient<MaterialCategory> { }
