namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public class PvpSeasonLeaderboardScoring
{
    public string Id { get; set; }
    public PvpSeasonLeaderboardScoringType Type { get; set; }
    public string Description { get; set; }
    public string Name { get; set; }
    public PvpSeasonLeaderboardScoringOrder Ordering { get; set; }
}
