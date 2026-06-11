namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpStats
{
    public int PvpRank { get; set; }
    public int PvpRankPoints { get; set; }
    public int PvpRankRollovers { get; set; }
    public PvpStatsAggregate Aggregate { get; set; } = null!;
    public Dictionary<string, PvpStatsAggregate> Professions { get; set; } = new();
    public Dictionary<string, PvpStatsAggregate> Ladders { get; set; } = new();
}
