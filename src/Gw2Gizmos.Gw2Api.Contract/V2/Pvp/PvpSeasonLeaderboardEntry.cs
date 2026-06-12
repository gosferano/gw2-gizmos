namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpSeasonLeaderboardEntry
{
    public string Name { get; set; } = null!;
    public int Rank { get; set; }
    public DateTimeOffset Date { get; set; }
    public PvpSeasonLeaderboardEntryScore[] Scores { get; set; } = Array.Empty<PvpSeasonLeaderboardEntryScore>();
}
