using Gw2Gizmos.Gw2Api.Contract.V2.Skills;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public class SkillFactJsonConverter : PolymorphicJsonConverter<SkillFact>
{
    protected override string TypePropertyName => "type";

    protected override Dictionary<string, Type> TypeMap { get; } =
        new()
        {
            { SkillFactType.Buff, typeof(SkillFactBuff) },
            { SkillFactType.AttributeAdjust, typeof(SkillFactAttributeAdjust) },
            { SkillFactType.ComboField, typeof(SkillFactComboField) },
            { SkillFactType.ComboFinisher, typeof(SkillFactComboFinisher) },
            { SkillFactType.Damage, typeof(SkillFactDamage) },
            { SkillFactType.Distance, typeof(SkillFactDistance) },
            { SkillFactType.Duration, typeof(SkillFactDuration) },
            { SkillFactType.Heal, typeof(SkillFactHeal) },
            { SkillFactType.HealingAdjust, typeof(SkillFactHealingAdjust) },
            { SkillFactType.NoData, typeof(SkillFactNoData) },
            { SkillFactType.Number, typeof(SkillFactNumber) },
            { SkillFactType.Percent, typeof(SkillFactPercent) },
            { SkillFactType.PrefixedBuff, typeof(SkillFactPrefixedBuff) },
            { SkillFactType.Radius, typeof(SkillFactRadius) },
            { SkillFactType.Range, typeof(SkillFactRange) },
            { SkillFactType.Recharge, typeof(SkillFactRecharge) },
            { SkillFactType.StunBreak, typeof(SkillFactStunBreak) },
            { SkillFactType.Time, typeof(SkillFactTime) },
            { SkillFactType.Unblockable, typeof(SkillFactUnblockable) },
        };

    protected override Type FallbackType => typeof(SkillFactNoData);
}
