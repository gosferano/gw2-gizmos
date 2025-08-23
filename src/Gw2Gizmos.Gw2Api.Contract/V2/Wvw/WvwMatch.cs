namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatch
{
    public string Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; }
    public WvwMatchTeamValues<int> Worlds { get; set; }
    public WvwMatchTeamValues<int[]> AllWorlds { get; set; }
    public WvwMatchTeamValues<int> Deaths { get; set; }
    public WvwMatchTeamValues<int> Kills { get; set; }
    public WvwMatchTeamValues<int> VictoryPoints { get; set; }
    public WvwMatchMap[] Maps { get; set; } = Array.Empty<WvwMatchMap>();
    public WvwMatchSkirmish[] Skirmishes { get; set; } = Array.Empty<WvwMatchSkirmish>();
}
