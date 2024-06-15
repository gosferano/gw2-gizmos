using Gw2Gizmos.Gw2Api.Contract.Colors;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Colors;

public class ColorsClient : BaseBulkAllClient<Color, int>, IColorsClient
{
    internal ColorsClient(IGw2ApiClient apiClient)
        : base(apiClient, "ids") { }

    protected override string UriPath => "/v2/colors";
}
