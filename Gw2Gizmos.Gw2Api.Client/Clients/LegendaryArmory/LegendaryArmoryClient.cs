using Gw2Gizmos.Gw2Api.Contract.LegendaryArmory;

namespace Gw2Gizmos.Gw2Api.Client.Clients.LegendaryArmory;

public class LegendaryArmoryClient : BaseBulkAllClient<LegendaryArmoryItem, int>, ILegendaryArmoryClient
{
    internal LegendaryArmoryClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/legendaryarmory";
}
