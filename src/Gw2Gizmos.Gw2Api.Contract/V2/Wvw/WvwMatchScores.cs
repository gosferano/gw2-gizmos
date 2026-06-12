namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwMatchScores
{
    public string Id { get; set; } = null!;
    public WvwMatchTeamValues<int> Scores { get; set; } = null!;
    public WvwMatchTeamValues<int> VictoryPoints { get; set; } = null!;
    public WvwMatchScoresMap[] Maps { get; set; } = Array.Empty<WvwMatchScoresMap>();
    public WvwMatchSkirmish[] Skirmishes { get; set; } = Array.Empty<WvwMatchSkirmish>();
}
