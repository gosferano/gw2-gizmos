namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwAbility
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public WvwAbilityRank[] Ranks { get; set; } = Array.Empty<WvwAbilityRank>();
}
