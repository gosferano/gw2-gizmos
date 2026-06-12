namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public sealed class TraitFactAttributeAdjust : TraitFact
{
    public int Value { get; set; }
    public string Target { get; set; } = null!;
}
