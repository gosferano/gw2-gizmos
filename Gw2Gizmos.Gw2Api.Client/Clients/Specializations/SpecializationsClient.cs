using Gw2Gizmos.Gw2Api.Contract.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Specializations;

public class SpecializationsClient : BaseBulkAllClient<Specialization, int>, ISpecializationsClient
{
    internal SpecializationsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/specializations";
}
