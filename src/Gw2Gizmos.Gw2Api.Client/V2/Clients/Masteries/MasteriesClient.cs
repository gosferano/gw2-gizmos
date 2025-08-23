using Gw2Gizmos.Gw2Api.Contract.V2.Masteries;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Masteries;

public class MasteriesClient : BaseBulkAllClient<Mastery, int>, IMasteriesClient
{
    internal MasteriesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/masteries";
}
