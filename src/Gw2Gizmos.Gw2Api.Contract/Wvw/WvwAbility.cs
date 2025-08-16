namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public class WvwAbility
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public WvwAbilityRank[] Ranks { get; set; } = Array.Empty<WvwAbilityRank>();
}
