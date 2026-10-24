using System.Text.Json;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// The wardrobe sync stores every skin from /v2/skins with its type-specific details, and every mini from /v2/minis.
/// </summary>
public sealed class WardrobeUpdaterTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();
    private readonly RoutingHttpStub _stub = new();

    public void Dispose() => _fixture.Dispose();

    // As the API returns them: an armor skin (a dye slot null where it can't be dyed), a weapon, a gathering tool
    // (without a description, as many skins are) and a back item.
    private const string Skins = """
        [
          {"id": 208, "name": "Scale Legs", "type": "Armor", "flags": ["ShowInWardrobe"], "restrictions": [],
           "icon": "https://x/208.png", "rarity": "Basic", "description": "",
           "details": {"type": "Leggings", "weight_class": "Heavy", "dye_slots": {
             "default": [{"color_id": 1, "material": "metal"}, null],
             "overrides": {"AsuraMale": [{"color_id": 2, "material": "leather"}]}}}},
          {"id": 4678, "name": "Bolt", "type": "Weapon", "flags": ["ShowInWardrobe"], "restrictions": [],
           "icon": "https://x/4678.png", "rarity": "Legendary", "description": "",
           "details": {"type": "Sword", "damage_type": "Lightning"}},
          {"id": 5810, "name": "Consortium Harvesting Sickle", "type": "Gathering", "flags": [], "restrictions": [],
           "icon": "https://x/5810.png", "rarity": "Rare", "details": {"type": "Foraging"}},
          {"id": 2394, "name": "Ad Infinitum", "type": "Back", "flags": [], "restrictions": ["Human"],
           "icon": "https://x/2394.png", "rarity": "Legendary", "description": "An infinite cape."}
        ]
        """;

    private async Task SyncAsync()
    {
        _stub.SetJson("/v2/skins", "[208, 4678, 5810, 2394]");
        _stub.SetJson("/v2/skins?ids=208,4678,5810,2394", Skins);
        _stub.SetJson(
            "/v2/minis",
            """[{"id": 1, "name": "Mini Rytlock", "icon": "https://x/m1.png", "order": 10, "item_id": 21047}]"""
        );
        _stub.SetJson("/v2/novelties", """[{"id": 1, "name": "Chair", "unlock_item": [70001, 70002]}]""");
        _stub.SetJson("/v2/outfits", """[{"id": 2, "name": "Outfit", "unlock_items": [70003]}]""");
        _stub.SetJson("/v2/jadebots", """[{"id": 3, "name": "Jade Bot", "unlock_item": 70004}]""");
        using Gw2GizmosDbContext db = _fixture.NewContext();
        await new WardrobeUpdater(db, _stub.BuildClientFactory(), NullLogger<WardrobeUpdater>.Instance)
            .UpdateWardrobe(CancellationToken.None);
    }

    [Fact]
    public void Stores_each_skin_with_its_type_specific_details()
    {
        SyncAsync().GetAwaiter().GetResult();

        using Gw2GizmosDbContext db = _fixture.NewContext();
        Dictionary<int, Skin> skins = db.Skins.AsNoTracking().ToDictionary(s => s.Id);

        Skin armor = skins[208];
        Assert.Equal(("Scale Legs", "Armor", "Basic"), (armor.Name, armor.Type, armor.Rarity));
        Assert.Equal(["ShowInWardrobe"], armor.Flags);
        Assert.Equal(("Leggings", "Heavy"), (armor.ArmorSlot, armor.WeightClass));
        using (JsonDocument dyes = JsonDocument.Parse(armor.DyeSlots!))
        {
            JsonElement slots = dyes.RootElement.GetProperty("default");
            Assert.Equal(1, slots[0].GetProperty("color_id").GetInt32());
            Assert.Equal(JsonValueKind.Null, slots[1].ValueKind);
            JsonElement asuraMale = dyes.RootElement.GetProperty("overrides").GetProperty("AsuraMale");
            Assert.Equal("leather", asuraMale[0].GetProperty("material").GetString());
        }

        Assert.Equal(
            ("Sword", "Lightning", (string?)null),
            (skins[4678].WeaponType, skins[4678].DamageType, skins[4678].ArmorSlot)
        );
        Assert.Equal(("Foraging", ""), (skins[5810].GatheringType, skins[5810].Description));
        Assert.Equal(["Human"], skins[2394].Restrictions);
        Assert.Equal("An infinite cape.", skins[2394].Description);
        Assert.Null(skins[2394].DyeSlots);
    }

    [Fact]
    public async Task Stores_every_mini()
    {
        await SyncAsync();

        Mini mini = await _fixture.NewContext().Minis.SingleAsync();
        Assert.Equal(("Mini Rytlock", 10, 21047), (mini.Name, mini.Order, mini.ItemId));
    }

    [Fact]
    public async Task Stores_the_items_that_unlock_each_novelty_outfit_and_jade_bot()
    {
        await SyncAsync();

        Assert.Equal(
            [("JadeBot", 3, 70004), ("Novelty", 1, 70001), ("Novelty", 1, 70002), ("Outfit", 2, 70003)],
            (await _fixture.NewContext().UnlockItems.ToListAsync()).Select(u => (u.Kind, u.UnlockId, u.ItemId))
                .Order().ToList());
    }
}
