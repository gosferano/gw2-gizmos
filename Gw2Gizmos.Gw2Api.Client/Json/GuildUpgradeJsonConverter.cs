using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public class GuildUpgradeJsonConverter : PolymorphicJsonConverter<GuildUpgrade>
{
    protected override string TypePropertyName { get; } = "type";

    protected override Dictionary<string, Type> TypeMap { get; } =
        new()
        {
            { GuildUpgradeType.AccumulatingCurrency, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.BankBag, typeof(GuildUpgradeBankBag) },
            { GuildUpgradeType.Boost, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.Claimable, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.Consumable, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.Decoration, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.GuildHall, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.GuildHallExpedition, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.Hub, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.Queue, typeof(GuildUpgradeSimple) },
            { GuildUpgradeType.Unlock, typeof(GuildUpgradeSimple) }
        };
}
