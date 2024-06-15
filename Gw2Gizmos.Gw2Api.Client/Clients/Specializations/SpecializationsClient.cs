using Gw2Gizmos.Gw2Api.Contract.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Specializations;

public class SpecializationsClient : BaseBulkAllClient<Specialization, int>, ISpecializationsClient
{
    internal SpecializationsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/specializations";
}
