using Gw2Gizmos.Gw2Api.Contract.V2.Skins;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public sealed class SkinJsonConverter : PolymorphicJsonConverter<Skin>
{
    protected override string TypePropertyName => "type";

    protected override Dictionary<string, Type> TypeMap { get; } =
        new()
        {
            { SkinType.Armor, typeof(SkinArmor) },
            { SkinType.Back, typeof(SkinBack) },
            { SkinType.Gathering, typeof(SkinGathering) },
            { SkinType.Weapon, typeof(SkinWeapon) },
        };
}
