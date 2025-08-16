namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwClient : IBlobClient<string[]>
{
    public IWvwAbilitiesClient Abilities { get; }
    public IWvwMatchesClient Matches { get; }
    public IWvwObjectivesClient Objectives { get; }
    public IWvwRanksClient Ranks { get; }
    public IWvwUpgradesClient Upgrades { get; }
}
