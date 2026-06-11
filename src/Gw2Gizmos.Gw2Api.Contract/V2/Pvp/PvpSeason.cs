namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpSeason
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public bool Active { get; set; }
    public PvpSeasonDivision[] Divisions { get; set; } = Array.Empty<PvpSeasonDivision>();
    public PvpSeasonRank[] Ranks { get; set; } = Array.Empty<PvpSeasonRank>();
    public Dictionary<string, PvpSeasonLeaderboard> Leaderboards { get; set; } = new();
}
