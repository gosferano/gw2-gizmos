using Gw2Gizmos.Gw2Api.Contract.Masteries;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Masteries;

public class MasteriesClient : BaseBulkAllClient<Mastery, int>, IMasteriesClient
{
    internal MasteriesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/masteries";
}
