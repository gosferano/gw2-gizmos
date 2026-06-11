namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpSeasonLeaderboardSettings
{
    public string Name { get; set; } = null!;
    public int? Duration { get; set; }
    public string Scoring { get; set; } = null!;
    public PvpSeasonLeaderboardSettingsTier[] Tiers { get; set; } = Array.Empty<PvpSeasonLeaderboardSettingsTier>();
}
