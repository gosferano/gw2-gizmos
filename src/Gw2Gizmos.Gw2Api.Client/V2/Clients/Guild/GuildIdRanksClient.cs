using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public class GuildIdRanksClient : BaseBlobClient<GuildRank[]>, IGuildIdRanksClient
{
    private readonly string _guildId;

    internal GuildIdRanksClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
    }

    protected override string UriPath => $"/v2/guild/{_guildId}/ranks";
}
