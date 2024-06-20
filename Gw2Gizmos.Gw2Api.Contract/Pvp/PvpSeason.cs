namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public class PvpSeason
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public bool Active { get; set; }
    public PvpSeasonDivision[] Divisions { get; set; } = Array.Empty<PvpSeasonDivision>();
    public PvpSeasonRank[] Ranks { get; set; } = Array.Empty<PvpSeasonRank>();
    public Dictionary<string, PvpSeasonLeaderboard> Leaderboards { get; set; } = new();
}
