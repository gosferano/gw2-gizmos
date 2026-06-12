namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public sealed class GuildSearchClient : BaseClient, IGuildSearchClient
{
    internal GuildSearchClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/guild/search";

    public IGuildSearchNameClient this[string guildName] => new GuildSearchNameClient(HttpClient, guildName);
}
