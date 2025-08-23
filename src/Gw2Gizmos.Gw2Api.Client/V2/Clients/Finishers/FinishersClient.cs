using Gw2Gizmos.Gw2Api.Contract.V2.Finishers;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Finishers;

public class FinishersClient : BaseBulkAllClient<Finisher, int>, IFinishersClient
{
    internal FinishersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/finishers";
}
