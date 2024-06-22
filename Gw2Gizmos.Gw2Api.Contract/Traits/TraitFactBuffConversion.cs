namespace Gw2Gizmos.Gw2Api.Contract.Traits;

public class TraitFactBuffConversion : TraitFact
{
    public AttributeName Source { get; set; }
    public AttributeName Target { get; set; }
    public int Percent { get; set; }
}
