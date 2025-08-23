namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpHero
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public PvpHeroStats Stats { get; set; }
    public string Overlay { get; set; }
    public string Underlay { get; set; }
    public PvpHeroSkin[] Skins { get; set; } = Array.Empty<PvpHeroSkin>();
}
