namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class GuildTeamSeason
{
    public string Id { get; set; } = null!;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Rating { get; set; }
}
