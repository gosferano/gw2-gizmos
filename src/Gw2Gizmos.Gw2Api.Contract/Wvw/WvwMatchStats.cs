namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwMatchStats
{
    public string Id { get; set; }
    public WvwMatchTeamValues<int> Kills { get; set; }
    public WvwMatchTeamValues<int> Deaths { get; set; }
    public WvwMatchStatsMap[] Maps { get; set; } = Array.Empty<WvwMatchStatsMap>();
}
