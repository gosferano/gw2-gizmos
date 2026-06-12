using Gw2Gizmos.Gw2Api.Contract.V2.Skills;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public sealed class TraitFactPrefixedBuff : TraitFactBuff
{
    public SkillFactPrefixedBuffPrefix Prefix { get; set; } = null!;
}
