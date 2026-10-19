using System.Text.Json;
using Gw2Gizmos.Gw2Api.Contract;
using Gw2Gizmos.Gw2Api.Contract.V2.Pvp;
using Gw2Gizmos.Gw2Api.Contract.V2.Skins;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

/// <summary>
/// Keys the API spells in PascalCase, which the contract's snake_case naming alone would never match (and so silently
/// drop): a skin's per-race dye slots and attribute bonuses.
/// </summary>
public class PascalCaseKeyTests
{
    private static readonly JsonSerializerOptions Options = Gw2ApiContractJson.Options;

    [Fact]
    public void An_armor_skin_reads_its_per_race_dye_slots_and_keeps_undyeable_slots_as_null()
    {
        // As the API returns Scale Legs-like armor: a null slot can't be dyed.
        const string json = """
            {"id": 10, "name": "X", "type": "Armor", "details": {"type": "Coat", "weight_class": "Light",
              "dye_slots": {"default": [{"color_id": 1, "material": "cloth"}, null],
                            "overrides": {"AsuraMale": [null, {"color_id": 2, "material": "leather"}],
                                          "SylvariFemale": [{"color_id": 3, "material": "metal"}]}}}}
            """;

        SkinArmor skin = Assert.IsType<SkinArmor>(JsonSerializer.Deserialize<Skin>(json, Options));

        SkinArmorDetailsDyeSlots slots = skin.Details.DyeSlots!;
        Assert.Equal(1, slots.Default[0]!.ColorId);
        Assert.Null(slots.Default[1]);
        Assert.Null(slots.Overrides.AsuraMale[0]);
        Assert.Equal("leather", slots.Overrides.AsuraMale[1]!.Material);
        Assert.Equal(3, Assert.Single(slots.Overrides.SylvariFemale)!.ColorId);
        Assert.Empty(slots.Overrides.HumanMale);
    }

    [Fact]
    public void Attribute_bonuses_read_two_word_attributes()
    {
        const string json = """
            {"id": 4, "name": "Assassin Amulet", "icon": "https://x/a.png",
             "attributes": {"Precision": 1200, "Power": 900, "CritDamage": 900, "ConditionDamage": 300,
                            "BoonDuration": 100, "ConditionDuration": 50}}
            """;

        PvpAmulet amulet = JsonSerializer.Deserialize<PvpAmulet>(json, Options)!;

        Assert.Equal(
            (1200m, 900m, 900m, 300m, 100m, 50m),
            (amulet.Attributes.Precision, amulet.Attributes.Power, amulet.Attributes.CritDamage,
                amulet.Attributes.ConditionDamage, amulet.Attributes.BoonDuration, amulet.Attributes.ConditionDuration)
        );
    }
}
