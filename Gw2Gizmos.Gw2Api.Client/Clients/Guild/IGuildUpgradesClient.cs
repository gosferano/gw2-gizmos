using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public interface IGuildUpgradesClient
    : IAllExpandableClient<GuildUpgrade>,
        IBulkExpandableClient<GuildUpgrade, int>,
        IPaginatedClient<GuildUpgrade>;
