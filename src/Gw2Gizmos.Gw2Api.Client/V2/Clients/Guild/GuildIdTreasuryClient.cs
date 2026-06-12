using Gw2Gizmos.Gw2Api.Contract.V2.Guild;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public sealed class GuildIdTreasuryClient : BaseBlobClient<GuildTreasuryItem[]>, IGuildIdTreasuryClient
{
    private readonly string _guildId;

    internal GuildIdTreasuryClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
    }

    protected override string UriPath => $"/v2/guild/{_guildId}/treasury";
}
