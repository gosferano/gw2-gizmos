namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwMatchStats
{
    public string Id { get; set; } = null!;
    public WvwMatchTeamValues<int> Kills { get; set; } = null!;
    public WvwMatchTeamValues<int> Deaths { get; set; } = null!;
    public WvwMatchStatsMap[] Maps { get; set; } = Array.Empty<WvwMatchStatsMap>();
}
