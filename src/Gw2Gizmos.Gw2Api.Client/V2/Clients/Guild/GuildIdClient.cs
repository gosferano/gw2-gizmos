namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public class GuildIdClient : BaseBlobClient<Contract.V2.Guild.Guild>, IGuildIdClient
{
    private readonly string _guildId;

    internal GuildIdClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
        Members = new GuildIdMembersClient(httpClient, guildId);
        Ranks = new GuildIdRanksClient(httpClient, guildId);
        Stash = new GuildIdStashClient(httpClient, guildId);
        Storage = new GuildIdStorageClient(httpClient, guildId);
        Teams = new GuildIdTeamsClient(httpClient, guildId);
        Treasury = new GuildIdTreasuryClient(httpClient, guildId);
        Upgrades = new GuildIdUpgradesClient(httpClient, guildId);
    }

    protected override string UriPath => $"/v2/guild/{_guildId}";

    public IGuildIdMembersClient Members { get; }
    public IGuildIdRanksClient Ranks { get; }
    public IGuildIdStashClient Stash { get; }
    public IGuildIdStorageClient Storage { get; }
    public IGuildIdTeamsClient Teams { get; }
    public IGuildIdTreasuryClient Treasury { get; }
    public IGuildIdUpgradesClient Upgrades { get; }
}
