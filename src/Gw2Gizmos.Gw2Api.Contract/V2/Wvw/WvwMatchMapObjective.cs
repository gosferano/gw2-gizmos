namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public sealed class WvwMatchMapObjective
{
    public string Id { get; set; } = null!;
    public WvwObjectiveType Type { get; set; }
    public WvwOwner Owner { get; set; }
    public DateTimeOffset LastFlipped { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public int PointsTick { get; set; }
    public int PointsCapture { get; set; }
    public int[] GuildUpgrades { get; set; } = Array.Empty<int>();
    public int? YaksDelivered { get; set; }
}
