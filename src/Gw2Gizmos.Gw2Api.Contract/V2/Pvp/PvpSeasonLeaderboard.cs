namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpSeasonLeaderboard
{
    public PvpSeasonLeaderboardSettings Settings { get; set; } = null!;
    public PvpSeasonLeaderboardScoring[] Scorings { get; set; } = Array.Empty<PvpSeasonLeaderboardScoring>();
}
