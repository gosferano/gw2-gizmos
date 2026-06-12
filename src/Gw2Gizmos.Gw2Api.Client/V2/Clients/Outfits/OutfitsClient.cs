using Gw2Gizmos.Gw2Api.Contract.V2.Outfits;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Outfits;

public sealed class OutfitsClient : BaseBulkAllClient<Outfit, int>, IOutfitsClient
{
    internal OutfitsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/outfits";
}
