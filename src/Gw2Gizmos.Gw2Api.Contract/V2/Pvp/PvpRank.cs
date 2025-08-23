namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public class PvpRank
{
    public int Id { get; set; }
    public int FinisherId { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public int MinRank { get; set; }
    public int MaxRank { get; set; }
    public PvpRankLevels[] Levels { get; set; } = Array.Empty<PvpRankLevels>();
}
