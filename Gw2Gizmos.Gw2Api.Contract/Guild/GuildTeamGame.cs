using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Contract.Guild;

public class GuildTeamGame
{
    public string Id { get; set; }
    public int MapId { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset Ended { get; set; }
    public string Result { get; set; }
    public string Team { get; set; }
    public PvpTeamScores Scores { get; set; }
    public PvpRatingType RatingType { get; set; }
}
