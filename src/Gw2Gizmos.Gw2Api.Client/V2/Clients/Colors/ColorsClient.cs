using Gw2Gizmos.Gw2Api.Contract.Colors;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Colors;

public class ColorsClient : BaseBulkAllClient<Color, int>, IColorsClient
{
    internal ColorsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/colors";
}
