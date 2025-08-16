using Gw2Gizmos.Gw2Api.Contract.Colors;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Colors;

public interface IColorsClient
    : IAllExpandableClient<Color>,
        IBulkExpandableClient<Color, int>,
        IPaginatedClient<Color> { }
