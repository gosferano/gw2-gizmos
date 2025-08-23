using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public class ItemJsonConverter : PolymorphicJsonConverter<Item>
{
    protected override string TypePropertyName { get; } = "type";

    protected override Dictionary<string, Type> TypeMap { get; } =
        new()
        {
            { ItemType.Armor, typeof(Armor) },
            { ItemType.Bag, typeof(Bag) },
            { ItemType.Back, typeof(BackItem) },
            { ItemType.Consumable, typeof(Consumable) },
            { ItemType.Container, typeof(Container) },
            { ItemType.Gathering, typeof(Gathering) },
            { ItemType.Gizmo, typeof(Gizmo) },
            { ItemType.MiniPet, typeof(MiniPet) },
            { ItemType.Tool, typeof(Tool) },
            { ItemType.Trinket, typeof(Trinket) },
            { ItemType.UpgradeComponent, typeof(UpgradeComponent) },
            { ItemType.Weapon, typeof(Weapon) },
            { ItemType.CraftingMaterial, typeof(ItemSimple) },
            { ItemType.JadeTechModule, typeof(ItemSimple) },
            { ItemType.Key, typeof(ItemSimple) },
            { ItemType.PowerCore, typeof(ItemSimple) },
            { ItemType.Relic, typeof(ItemSimple) },
            { ItemType.Trait, typeof(ItemSimple) },
            { ItemType.Trophy, typeof(ItemSimple) }
        };
}
