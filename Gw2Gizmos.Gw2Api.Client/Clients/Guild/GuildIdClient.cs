namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildIdClient : BaseBlobClient<Contract.Guild.Guild>, IGuildIdClient
{
    private readonly string _guildId;

    internal GuildIdClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
    }

    protected override string UriPath => $"/v2/guild/{_guildId}";
}
