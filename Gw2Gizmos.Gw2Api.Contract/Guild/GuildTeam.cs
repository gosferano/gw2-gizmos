using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Contract.Guild;

public class GuildTeam
{
    public int Id { get; set; }
    public GuildTeamMember[] Members { get; set; } = Array.Empty<GuildTeamMember>();
    public string Name { get; set; }
    public string State { get; set; }
    public PvpStatsAggregate Aggregate { get; set; }
    public Dictionary<string, PvpStatsAggregate> Ladders { get; set; } = new();
    public GuildTeamGame[] Games { get; set; } = Array.Empty<GuildTeamGame>();
    public GuildTeamSeason[] Seasons { get; set; } = Array.Empty<GuildTeamSeason>();
}
