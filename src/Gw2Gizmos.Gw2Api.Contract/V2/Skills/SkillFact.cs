namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public class SkillFact
{
    /// <summary>
    /// The fact's label, e.g. "Damage". Some facts come without one, mostly Number, BuffArray and AttributeAdjust
    /// facts (e.g. Magnet's, Cyclone's, Lich Form's).
    /// </summary>
    public string? Text { get; set; }
    public string Icon { get; set; } = null!;
    public SkillFactType Type { get; set; }
    public int? RequiresTrait { get; set; }
    public int? Overrides { get; set; }
}
