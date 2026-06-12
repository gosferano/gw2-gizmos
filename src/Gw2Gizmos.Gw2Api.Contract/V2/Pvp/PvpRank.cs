namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpRank
{
    public int Id { get; set; }
    public int FinisherId { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int MinRank { get; set; }
    public int MaxRank { get; set; }
    public PvpRankLevels[] Levels { get; set; } = Array.Empty<PvpRankLevels>();
}
