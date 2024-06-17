using Gw2Gizmos.Gw2Api.Contract.Finishers;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Finishers;

public class FinishersClient : BaseBulkAllClient<Finisher, int>, IFinishersClient
{
    internal FinishersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/finishers";
}
