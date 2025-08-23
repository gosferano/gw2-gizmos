namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public class TraitFact
{
    public string? Text { get; set; }
    public string? Icon { get; set; }
    public TraitFactType Type { get; set; }
    public int? RequiresTrait { get; set; }
    public int? Overrides { get; set; }
}
