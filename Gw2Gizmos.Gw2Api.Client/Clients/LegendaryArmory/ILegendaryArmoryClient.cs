using Gw2Gizmos.Gw2Api.Contract.LegendaryArmory;

namespace Gw2Gizmos.Gw2Api.Client.Clients.LegendaryArmory;

public interface ILegendaryArmoryClient
    : IAllExpandableClient<LegendaryArmoryItem>,
        IBulkExpandableClient<LegendaryArmoryItem, int>,
        IPaginatedClient<LegendaryArmoryItem>;
