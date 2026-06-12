namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpGame
{
    public string Id { get; set; } = null!;
    public int MapId { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset Ended { get; set; }
    public PvpResult Result { get; set; }
    public PvpTeam Team { get; set; }
    public ProfessionName Profession { get; set; }
    public PvpTeamScores Scores { get; set; } = null!;
    public PvpRatingType RatingType { get; set; }
    public int RatingChange { get; set; }
    public string? Season { get; set; }
}
