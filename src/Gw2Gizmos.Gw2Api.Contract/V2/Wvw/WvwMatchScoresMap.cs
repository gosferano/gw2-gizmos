namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwMatchScoresMap
{
    public int Id { get; set; }
    public WvwMapType Type { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; } = null!;
}
