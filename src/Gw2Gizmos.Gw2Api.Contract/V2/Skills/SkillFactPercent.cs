namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public sealed class SkillFactPercent : SkillFact
{
    /// <summary>
    /// The percentage. A few facts give it as <c>value</c> instead, e.g. Engage Photon Forge's "Maximum Heat".
    /// </summary>
    public decimal? Percent { get; set; }

    /// <summary>The percentage, on the few facts that give it here rather than as <see cref="Percent"/>.</summary>
    public decimal? Value { get; set; }
}
