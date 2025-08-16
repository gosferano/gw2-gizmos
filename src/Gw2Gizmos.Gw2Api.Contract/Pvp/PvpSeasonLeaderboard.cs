namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public class PvpSeasonLeaderboard
{
    public PvpSeasonLeaderboardSettings Settings { get; set; }
    public PvpSeasonLeaderboardScoring[] Scorings { get; set; } = Array.Empty<PvpSeasonLeaderboardScoring>();
}
