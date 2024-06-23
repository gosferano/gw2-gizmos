namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwMatchMap
{
    public int Id { get; set; }
    public WvwMapType Type { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; }
    public WvwMatchMapBonus[] Bonuses { get; set; } = Array.Empty<WvwMatchMapBonus>();
    public WvwMatchMapObjective[] Objectives { get; set; } = Array.Empty<WvwMatchMapObjective>();
}
