namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatchScores
{
    public string Id { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; }
    public WvwMatchTeamValues<int> VictoryPoints { get; set; }
    public WvwMatchScoresMap[] Maps { get; set; } = Array.Empty<WvwMatchScoresMap>();
    public WvwMatchSkirmish[] Skirmishes { get; set; } = Array.Empty<WvwMatchSkirmish>();
}
