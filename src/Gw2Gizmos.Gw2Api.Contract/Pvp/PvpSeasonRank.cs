namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public class PvpSeasonRank
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string Overlay { get; set; }
    public string OverlaySmall { get; set; }
    public PvpSeasonRankTier[] Tiers { get; set; } = Array.Empty<PvpSeasonRankTier>();
}
