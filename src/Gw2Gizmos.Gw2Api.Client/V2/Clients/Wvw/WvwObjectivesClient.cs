using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwObjectivesClient : BaseBulkAllClient<WvwObjective, string>, IWvwObjectivesClient
{
    internal WvwObjectivesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/objectives";
}
