namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpHero
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public PvpHeroStats Stats { get; set; } = null!;
    public string Overlay { get; set; } = null!;
    public string Underlay { get; set; } = null!;
    public PvpHeroSkin[] Skins { get; set; } = Array.Empty<PvpHeroSkin>();
}
