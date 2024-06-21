using Gw2Gizmos.Gw2Api.Contract.Skins;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public class SkinJsonConverter : PolymorphicJsonConverter<Skin>
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
