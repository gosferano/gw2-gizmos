namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwMatchScoresMap
{
    public int Id { get; set; }
    public WvwMapType Type { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; }
}
