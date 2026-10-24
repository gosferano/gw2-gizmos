using System.Text.Json;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Contract;

namespace Gw2Gizmos.Data.Worker.Tests;

public sealed class ItemEntityMapperTests
{
    [Fact]
    public void A_weapon_keeps_its_default_skin()
    {
        // As the API returns it, trimmed to what's mapped.
        const string json = """
            {"id": 30698, "name": "The Bifrost", "type": "Weapon", "level": 80, "rarity": "Legendary",
             "vendor_value": 100000, "default_skin": 4678, "game_types": ["Activity"], "flags": ["HideSuffix"],
             "restrictions": [], "chat_link": "[&AgEqeAAA]", "icon": "https://x/bifrost.png",
             "details": {"type": "Staff", "damage_type": "Physical", "min_power": 1045, "max_power": 1155,
                         "defense": 0, "infusion_slots": [], "attribute_adjustment": 0, "secondary_suffix_item_id": ""}}
            """;
        var apiItem = JsonSerializer.Deserialize<Gw2Api.Contract.V2.Items.Item>(json, Gw2ApiContractJson.Options)!;

        Item? item = ItemEntityMapper.MapApiItem(apiItem);

        Assert.Equal(4678, item?.DefaultSkin);
    }
}
