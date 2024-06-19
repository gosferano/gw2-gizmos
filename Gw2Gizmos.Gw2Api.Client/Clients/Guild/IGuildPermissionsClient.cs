using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public interface IGuildPermissionsClient
    : IAllExpandableClient<GuildPermission>,
        IBulkExpandableClient<GuildPermission, string>,
        IPaginatedClient<GuildPermission>;
