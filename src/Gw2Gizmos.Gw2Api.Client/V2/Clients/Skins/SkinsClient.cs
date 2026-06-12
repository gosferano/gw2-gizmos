using Gw2Gizmos.Gw2Api.Contract.V2.Skins;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Skins;

public sealed class SkinsClient : BaseBulkClient<Skin, int>, ISkinsClient
{
    internal SkinsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/skins";
}
