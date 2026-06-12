using Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildTeamGame
{
    public string Id { get; set; } = null!;
    public int MapId { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset Ended { get; set; }
    public PvpResult Result { get; set; }
    public PvpTeam Team { get; set; }
    public PvpTeamScores Scores { get; set; } = null!;
    public PvpRatingType RatingType { get; set; }
}
