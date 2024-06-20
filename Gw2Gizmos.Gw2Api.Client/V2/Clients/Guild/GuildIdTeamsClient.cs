using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public class GuildIdTeamsClient : BaseBlobClient<GuildTeam[]>, IGuildIdTeamsClient
{
    private readonly string _guildId;

    internal GuildIdTeamsClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
    }

    protected override string UriPath => $"/v2/guild/{_guildId}/teams";
}
