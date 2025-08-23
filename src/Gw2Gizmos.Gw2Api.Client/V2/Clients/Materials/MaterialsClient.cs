using Gw2Gizmos.Gw2Api.Contract.V2.Materials;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Materials;

public class MaterialsClient : BaseBulkAllClient<MaterialCategory, int>, IMaterialsClient
{
    internal MaterialsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/materials";
}
