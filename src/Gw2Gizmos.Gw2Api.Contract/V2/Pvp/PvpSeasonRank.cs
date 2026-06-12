namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpSeasonRank
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Overlay { get; set; } = null!;
    public string OverlaySmall { get; set; } = null!;
    public PvpSeasonRankTier[] Tiers { get; set; } = Array.Empty<PvpSeasonRankTier>();
}
