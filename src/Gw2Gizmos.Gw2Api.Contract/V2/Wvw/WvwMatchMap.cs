namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatchMap
{
    public int Id { get; set; }
    public WvwMapType Type { get; set; }
    public WvwMatchTeamValues<int> Scores { get; set; } = null!;
    public WvwMatchMapBonus[] Bonuses { get; set; } = Array.Empty<WvwMatchMapBonus>();
    public WvwMatchMapObjective[] Objectives { get; set; } = Array.Empty<WvwMatchMapObjective>();
}
