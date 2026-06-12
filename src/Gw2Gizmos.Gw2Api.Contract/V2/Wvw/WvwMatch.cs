namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwMatch
{
    public string Id { get; set; } = null!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; } = null!;
    public WvwMatchTeamValues<int> Worlds { get; set; } = null!;
    public WvwMatchTeamValues<int[]> AllWorlds { get; set; } = null!;
    public WvwMatchTeamValues<int> Deaths { get; set; } = null!;
    public WvwMatchTeamValues<int> Kills { get; set; } = null!;
    public WvwMatchTeamValues<int> VictoryPoints { get; set; } = null!;
    public WvwMatchMap[] Maps { get; set; } = Array.Empty<WvwMatchMap>();
    public WvwMatchSkirmish[] Skirmishes { get; set; } = Array.Empty<WvwMatchSkirmish>();
}
