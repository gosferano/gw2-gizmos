namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpSeasonDivision
{
    public string Name { get; set; } = null!;
    public PvpSeasonFlag[] Flags { get; set; } = Array.Empty<PvpSeasonFlag>();
    public string LargeIcon { get; set; } = null!;
    public string SmallIcon { get; set; } = null!;
    public string PipIcon { get; set; } = null!;
    public PvpSeasonDivisionTier[] Tiers { get; set; } = Array.Empty<PvpSeasonDivisionTier>();
}
