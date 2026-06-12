namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwMatchSkirmish
{
    public int Id { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; } = null!;
    public WvwMatchSkirmishMapScore[] MapScores { get; set; } = Array.Empty<WvwMatchSkirmishMapScore>();
}
