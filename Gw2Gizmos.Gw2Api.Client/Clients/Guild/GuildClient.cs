namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildClient : BaseClient, IGuildClient
{
    internal GuildClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/guilds";

    public IGuildIdClient this[string guildId] => new GuildIdClient(HttpClient, guildId);
}
