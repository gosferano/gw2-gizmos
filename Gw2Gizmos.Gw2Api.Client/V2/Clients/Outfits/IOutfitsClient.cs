using Gw2Gizmos.Gw2Api.Contract.Outfits;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Outfits;

public interface IOutfitsClient
    : IAllExpandableClient<Outfit>,
        IBulkExpandableClient<Outfit, int>,
        IPaginatedClient<Outfit>;
