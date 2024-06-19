using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildPermissionsClient : BaseBulkAllClient<GuildPermission, string>, IGuildPermissionsClient
{
    internal GuildPermissionsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/guild/permissions";
}
