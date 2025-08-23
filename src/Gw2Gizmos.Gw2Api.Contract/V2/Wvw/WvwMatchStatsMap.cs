namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatchStatsMap
{
    public int Id { get; set; }
    public WvwMapType Type { get; set; }
    public WvwMatchTeamValues<int> Kills { get; set; }
    public WvwMatchTeamValues<int> Deaths { get; set; }
}
