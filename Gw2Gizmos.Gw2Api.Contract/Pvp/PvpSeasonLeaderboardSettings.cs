namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public class PvpSeasonLeaderboardSettings
{
    public string Name { get; set; }
    public int? Duration { get; set; }
    public string Scoring { get; set; }
    public PvpSeasonLeaderboardSettingsTier[] Tiers { get; set; } = Array.Empty<PvpSeasonLeaderboardSettingsTier>();
}
