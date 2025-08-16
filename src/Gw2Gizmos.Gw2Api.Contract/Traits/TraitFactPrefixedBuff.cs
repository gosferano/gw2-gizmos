using Gw2Gizmos.Gw2Api.Contract.Skills;

namespace Gw2Gizmos.Gw2Api.Contract.Traits;

public class TraitFactPrefixedBuff : TraitFactBuff
{
    public SkillFactPrefixedBuffPrefix Prefix { get; set; }
}
