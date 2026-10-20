namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public sealed class SkillFactPercent : SkillFact
{
    /// <summary>
    /// The percentage. A few facts give it as <c>value</c> instead, e.g. Engage Photon Forge's "Maximum Heat".
    /// </summary>
    public decimal? Percent { get; set; }
}
