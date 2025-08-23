using Gw2Gizmos.Gw2Api.Contract.V2.LegendaryArmory;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.LegendaryArmory;

public interface ILegendaryArmoryClient
    : IAllExpandableClient<LegendaryArmoryItem>,
        IBulkExpandableClient<LegendaryArmoryItem, int>,
        IPaginatedClient<LegendaryArmoryItem>;
