namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpSeasonLeaderboardScoring
{
    public string Id { get; set; } = null!;
    public PvpSeasonLeaderboardScoringType Type { get; set; }
    public string Description { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PvpSeasonLeaderboardScoringOrder Ordering { get; set; }
}
