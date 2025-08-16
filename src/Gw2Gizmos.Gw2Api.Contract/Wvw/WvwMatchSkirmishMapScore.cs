namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwMatchSkirmishMapScore
{
    public WvwMapType Type { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; }
}
