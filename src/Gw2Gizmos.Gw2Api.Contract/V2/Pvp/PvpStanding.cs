namespace Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

public sealed class PvpStanding
{
    public string SeasonId { get; set; } = null!;
    public PvpStandingCurrent Current { get; set; } = null!;
    public PvpStandingBest Best { get; set; } = null!;
}

public sealed class PvpStandingBest
{
    public int TotalPoints { get; set; }
    public int Division { get; set; }
    public int Tier { get; set; }
    public int Points { get; set; }
    public int Repeats { get; set; }
}

public sealed class PvpStandingCurrent
{
    public int TotalPoints { get; set; }
    public int Division { get; set; }
    public int Tier { get; set; }
    public int Points { get; set; }
    public int Repeats { get; set; }
    public int? Rating { get; set; }
    public int? Decay { get; set; }
}
