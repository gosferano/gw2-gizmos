namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildSearchClient : BaseClient, IGuildSearchClient
{
    internal GuildSearchClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/guild/search";

    public IGuildSearchNameClient this[string guildName] => new GuildSearchNameClient(HttpClient, guildName);
}
