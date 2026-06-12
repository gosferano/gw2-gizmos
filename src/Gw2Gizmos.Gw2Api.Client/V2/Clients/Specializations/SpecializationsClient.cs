using Gw2Gizmos.Gw2Api.Contract.V2.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Specializations;

public sealed class SpecializationsClient : BaseBulkAllClient<Specialization, int>, ISpecializationsClient
{
    internal SpecializationsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/specializations";
}
