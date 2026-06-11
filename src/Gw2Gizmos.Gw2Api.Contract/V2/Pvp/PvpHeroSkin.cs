namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpHeroSkin
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public bool Default { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
}
