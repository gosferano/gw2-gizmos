namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public sealed class GuildClient : BaseClient, IGuildClient
{
    internal GuildClient(HttpClient httpClient)
        : base(httpClient)
    {
        Permissions = new GuildPermissionsClient(httpClient);
        Search = new GuildSearchClient(httpClient);
        Upgrades = new GuildUpgradesClient(httpClient);
    }

    protected override string UriPath => "/v2/guilds";

    public IGuildIdClient this[string guildId] => new GuildIdClient(HttpClient, guildId);
    public IGuildPermissionsClient Permissions { get; }
    public IGuildSearchClient Search { get; }
    public IGuildUpgradesClient Upgrades { get; }
}
