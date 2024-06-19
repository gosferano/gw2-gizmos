namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public interface IGuildIdClient : IBlobClient<Contract.Guild.Guild>
{
    public IGuildIdMembersClient Members { get; }
    public IGuildIdRanksClient Ranks { get; }
    public IGuildIdStashClient Stash { get; }
    public IGuildIdStorageClient Storage { get; }
    public IGuildIdTeamsClient Teams { get; }
    public IGuildIdTreasuryClient Treasury { get; }
    public IGuildIdUpgradesClient Upgrades { get; }
}
