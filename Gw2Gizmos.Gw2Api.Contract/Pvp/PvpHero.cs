namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

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

public class PvpHeroSkin
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool Default { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
}

public class PvpHeroStats
{
    public int Offense { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
}
