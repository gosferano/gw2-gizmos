namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatchOverview
{
    public string Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public WvwMatchTeamValues<int> Worlds { get; set; }
    public WvwMatchTeamValues<int[]> AllWorlds { get; set; }
}
