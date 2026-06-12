namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public sealed class TraitFactBuffConversion : TraitFact
{
    public AttributeName Source { get; set; }
    public AttributeName Target { get; set; }
    public int Percent { get; set; }
}
