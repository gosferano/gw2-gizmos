namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwMatchSkirmish
{
    public int Id { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; }
    public WvwMatchSkirmishMapScore[] MapScores { get; set; } = Array.Empty<WvwMatchSkirmishMapScore>();
}
