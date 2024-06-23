namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwClient : BaseBlobClient<string[]>, IWvwClient
{
    internal WvwClient(HttpClient httpClient)
        : base(httpClient)
    {
        Abilities = new WvwAbilitiesClient(httpClient);
        Matches = new WvwMatchesClient(httpClient);
        Objectives = new WvwObjectivesClient(httpClient);
        Ranks = new WvwRanksClient(httpClient);
        Upgrades = new WvwUpgradesClient(httpClient);
    }

    protected override string UriPath => "/v2/wvw";

    public IWvwAbilitiesClient Abilities { get; }
    public IWvwMatchesClient Matches { get; }
    public IWvwObjectivesClient Objectives { get; }
    public IWvwRanksClient Ranks { get; }
    public IWvwUpgradesClient Upgrades { get; }
}
