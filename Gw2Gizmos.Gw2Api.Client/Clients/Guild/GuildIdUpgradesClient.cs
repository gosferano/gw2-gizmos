namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildIdUpgradesClient : BaseBlobClient<int[]>, IGuildIdUpgradesClient
{
    private readonly string _guildId;

    internal GuildIdUpgradesClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
    }

    protected override string UriPath => $"/v2/guild/{_guildId}/upgrades";
}
