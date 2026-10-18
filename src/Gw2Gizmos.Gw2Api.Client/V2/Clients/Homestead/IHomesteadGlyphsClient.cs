using Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

public interface IHomesteadGlyphsClient
    : IAllExpandableClient<HomesteadGlyph>,
        IBulkExpandableClient<HomesteadGlyph, string>,
        IPaginatedClient<HomesteadGlyph>;
