using Gw2Gizmos.Gw2Api.Contract.V2.Traits;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public sealed class TraitFactJsonConverter : PolymorphicJsonConverter<TraitFact>
{
    protected override string TypePropertyName => "type";

    protected override Dictionary<string, Type> TypeMap { get; } =
        new Dictionary<string, Type>()
        {
            { TraitFactType.AttributeAdjust, typeof(TraitFactAttributeAdjust) },
            { TraitFactType.Buff, typeof(TraitFactBuff) },
            { TraitFactType.BuffConversion, typeof(TraitFactBuffConversion) },
            { TraitFactType.ComboField, typeof(TraitFactComboField) },
            { TraitFactType.ComboFinisher, typeof(TraitFactComboFinisher) },
            { TraitFactType.Damage, typeof(TraitFactDamage) },
            { TraitFactType.Distance, typeof(TraitFactDistance) },
            { TraitFactType.NoData, typeof(TraitFactNoData) },
            { TraitFactType.Number, typeof(TraitFactNumber) },
            { TraitFactType.Percent, typeof(TraitFactPercent) },
            { TraitFactType.PrefixedBuff, typeof(TraitFactPrefixedBuff) },
            { TraitFactType.Radius, typeof(TraitFactRadius) },
            { TraitFactType.Range, typeof(TraitFactRange) },
            { TraitFactType.Recharge, typeof(TraitFactRecharge) },
            { TraitFactType.Time, typeof(TraitFactTime) },
            { TraitFactType.Unblockable, typeof(TraitFactUnblockable) }
        };
}
