namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public class TraitFactBuff : TraitFact
{
    public int Duration { get; set; }
    public string Status { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int ApplyCount { get; set; }
}
