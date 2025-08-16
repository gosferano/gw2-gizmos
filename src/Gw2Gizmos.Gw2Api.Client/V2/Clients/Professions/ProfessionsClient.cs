using Gw2Gizmos.Gw2Api.Contract.Professions;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Professions;

public class ProfessionsClient : BaseBulkAllClient<Profession, string>, IProfessionsClient
{
    internal ProfessionsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/professions";
}
