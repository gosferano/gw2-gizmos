using Gw2Gizmos.Gw2Api.Contract.Materials;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Materials;

public class MaterialsClient : BaseBulkAllClient<MaterialCategory, int>, IMaterialsClient
{
    internal MaterialsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/materials";
}
