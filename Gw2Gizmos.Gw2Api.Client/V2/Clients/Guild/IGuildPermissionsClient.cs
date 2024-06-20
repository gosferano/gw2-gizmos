using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public interface IGuildPermissionsClient
    : IAllExpandableClient<GuildPermission>,
        IBulkExpandableClient<GuildPermission, string>,
        IPaginatedClient<GuildPermission>;
