using Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class GuildTeamGame
{
    public string Id { get; set; }
    public int MapId { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset Ended { get; set; }
    public PvpResult Result { get; set; }
    public PvpTeam Team { get; set; }
    public PvpTeamScores Scores { get; set; }
    public PvpRatingType RatingType { get; set; }
}
