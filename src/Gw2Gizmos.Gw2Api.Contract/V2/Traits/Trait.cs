namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public sealed class Trait
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int Specialization { get; set; }
    public int Tier { get; set; }
    public TraitSlot Slot { get; set; }
    public TraitFact[] Facts { get; set; } = Array.Empty<TraitFact>();
    public TraitFact[] TraitedFacts { get; set; } = Array.Empty<TraitFact>();
    public TraitSkill[] Skills { get; set; } = Array.Empty<TraitSkill>();
}
