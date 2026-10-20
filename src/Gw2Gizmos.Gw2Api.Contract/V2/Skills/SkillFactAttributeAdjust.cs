namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public sealed class SkillFactAttributeAdjust : SkillFact
{
    /// <summary>
    /// The attribute adjusted. Missing where the adjustment isn't to an attribute, e.g. Path of Gluttony's
    /// "First-Hit Healing".
    /// </summary>
    public AttributeName? Target { get; set; }

    /// <summary>
    /// How much it's adjusted by. Missing on a broken record or two, e.g. the monster skill Blood Frenzy's.
    /// </summary>
    public int? Value { get; set; }
}
