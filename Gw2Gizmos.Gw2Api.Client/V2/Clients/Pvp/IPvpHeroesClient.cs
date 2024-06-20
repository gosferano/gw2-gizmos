using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpHeroesClient
    : IAllExpandableClient<PvpHero>,
        IBulkExpandableClient<PvpHero, string>,
        IPaginatedClient<PvpHero>;
