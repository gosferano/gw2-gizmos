namespace Gw2Gizmos.Gw2Api.Contract.Traits;

public class Trait
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public int Specialization { get; set; }
    public int Tier { get; set; }
    public TraitSlot Slot { get; set; }
    public TraitFact[] Facts { get; set; } = Array.Empty<TraitFact>();
    public TraitFact[] TraitedFacts { get; set; } = Array.Empty<TraitFact>();
    public TraitSkill[] Skills { get; set; } = Array.Empty<TraitSkill>();
}
