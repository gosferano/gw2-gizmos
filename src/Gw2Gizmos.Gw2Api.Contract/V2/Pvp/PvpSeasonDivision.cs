namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpSeasonDivision
{
    public string Name { get; set; }
    public PvpSeasonFlag[] Flags { get; set; } = Array.Empty<PvpSeasonFlag>();
    public string LargeIcon { get; set; }
    public string SmallIcon { get; set; }
    public string PipIcon { get; set; }
    public PvpSeasonDivisionTier[] Tiers { get; set; } = Array.Empty<PvpSeasonDivisionTier>();
}
