using System.Text.Json;
using Gw2Gizmos.Gw2Api.Contract.Json;
using Gw2Gizmos.Gw2Api.Contract;
using Gw2Gizmos.Gw2Api.Contract.V2;
using Gw2Gizmos.Gw2Api.Contract.V2.Guild;
using Gw2Gizmos.Gw2Api.Contract.V2.Items;
using Gw2Gizmos.Gw2Api.Contract.V2.Professions;
using Gw2Gizmos.Gw2Api.Contract.V2.Skills;
using Gw2Gizmos.Gw2Api.Contract.V2.Skins;
using Gw2Gizmos.Gw2Api.Contract.V2.Traits;

namespace Gw2Gizmos.Gw2Api.Client.Tests;

public class ConverterTests
{
    // The exact serializer config the client uses in production (all converters + source-gen context).
    private static readonly JsonSerializerOptions Options = Gw2ApiContractJson.Options;

    [Theory]
    [InlineData("Armor", typeof(Armor))]
    [InlineData("Weapon", typeof(Weapon))]
    [InlineData("Trinket", typeof(Trinket))]
    [InlineData("UpgradeComponent", typeof(UpgradeComponent))]
    [InlineData("CraftingMaterial", typeof(ItemSimple))] // several "simple" item types collapse to ItemSimple
    public void Item_dispatches_to_the_concrete_type(string type, Type expected)
    {
        Item? item = JsonSerializer.Deserialize<Item>($$"""{"id":1,"type":"{{type}}","name":"X"}""", Options);
        Assert.IsType(expected, item);
    }

    [Fact]
    public void Item_throws_on_an_unknown_discriminator()
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Item>("""{"id":1,"type":"Bogus","name":"X"}""", Options)
        );
    }

    [Fact]
    public void Item_throws_when_the_discriminator_is_missing()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Item>("""{"id":1,"name":"X"}""", Options));
    }

    [Theory]
    [InlineData("Armor", typeof(SkinArmor))]
    [InlineData("Weapon", typeof(SkinWeapon))]
    [InlineData("Gathering", typeof(SkinGathering))]
    [InlineData("Back", typeof(SkinBack))]
    public void Skin_dispatches_to_the_concrete_type(string type, Type expected)
    {
        Skin? skin = JsonSerializer.Deserialize<Skin>($$"""{"id":1,"type":"{{type}}","name":"X"}""", Options);
        Assert.IsType(expected, skin);
    }

    [Theory]
    [InlineData("Buff", typeof(SkillFactBuff))]
    [InlineData("Damage", typeof(SkillFactDamage))]
    [InlineData("Range", typeof(SkillFactRange))]
    [InlineData("PrefixedBuff", typeof(SkillFactPrefixedBuff))]
    [InlineData("NoData", typeof(SkillFactNoData))] // "NoData" is a real GW2 fact type, mapped explicitly
    public void SkillFact_dispatches_to_the_concrete_type(string type, Type expected)
    {
        SkillFact? fact = JsonSerializer.Deserialize<SkillFact>($$"""{"type":"{{type}}"}""", Options);
        Assert.IsType(expected, fact);
    }

    [Fact]
    public void SkillFact_throws_on_an_unknown_discriminator()
    {
        // Strict like every other converter: an unrecognized fact type fails loudly rather than degrading.
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<SkillFact>("""{"type":"SomeFutureFact"}""", Options)
        );
    }

    [Theory]
    [InlineData("Buff", typeof(TraitFactBuff))]
    [InlineData("Damage", typeof(TraitFactDamage))]
    [InlineData("BuffConversion", typeof(TraitFactBuffConversion))]
    public void TraitFact_dispatches_to_the_concrete_type(string type, Type expected)
    {
        TraitFact? fact = JsonSerializer.Deserialize<TraitFact>($$"""{"type":"{{type}}"}""", Options);
        Assert.IsType(expected, fact);
    }

    [Theory]
    [InlineData("BankBag", typeof(GuildUpgradeBankBag))]
    [InlineData("Boost", typeof(GuildUpgradeSimple))] // most guild-upgrade types share GuildUpgradeSimple
    public void GuildUpgrade_dispatches_to_the_concrete_type(string type, Type expected)
    {
        GuildUpgrade? upgrade = JsonSerializer.Deserialize<GuildUpgrade>(
            $$"""{"id":1,"type":"{{type}}","name":"X"}""",
            Options
        );
        Assert.IsType(expected, upgrade);
    }

    [Theory]
    [InlineData("Skill", typeof(ProfessionTrainingTrackStepSkill))]
    [InlineData("Trait", typeof(ProfessionTrainingTrackStepTrait))]
    public void ProfessionTrainingTrackStep_dispatches_to_the_concrete_type(string type, Type expected)
    {
        ProfessionTrainingTrackStep? step = JsonSerializer.Deserialize<ProfessionTrainingTrackStep>(
            $$"""{"type":"{{type}}"}""",
            Options
        );
        Assert.IsType(expected, step);
    }

    [Fact]
    public void Polymorphic_write_uses_the_runtime_type_so_it_round_trips()
    {
        Item? original = JsonSerializer.Deserialize<Item>(
            """{"id":7,"type":"Weapon","name":"Sword","rarity":"Rare"}""",
            Options
        );

        string json = JsonSerializer.Serialize(original, Options); // Write() serializes by runtime type
        Item? again = JsonSerializer.Deserialize<Item>(json, Options);

        Assert.IsType<Weapon>(again);
        Assert.Equal(7, again!.Id);
    }

    // --- scalar converters (tested in isolation; they need no DTO metadata) ---

    private static readonly JsonSerializerOptions NullableIntOptions = new() { Converters = { new NullableInt32Converter() } };

    [Theory]
    [InlineData("\"42\"", 42)] // GW2 sometimes returns an int as a numeric string
    [InlineData("42", 42)]
    public void NullableInt32_reads_numbers_and_numeric_strings(string json, int expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<int?>(json, NullableIntOptions));
    }

    [Theory]
    [InlineData("\"\"")] // empty string (the secondary_suffix_item_id "absent" case)
    [InlineData("\"   \"")]
    [InlineData("null")]
    public void NullableInt32_maps_empty_and_null_to_null(string json)
    {
        Assert.Null(JsonSerializer.Deserialize<int?>(json, NullableIntOptions));
    }

    private static readonly JsonSerializerOptions StringStructOptions = new()
    {
        Converters = { new StringValueStructConverterFactory() },
    };

    [Fact]
    public void StringValueStruct_round_trips_as_a_bare_string()
    {
        ItemRarity rarity = JsonSerializer.Deserialize<ItemRarity>("\"Exotic\"", StringStructOptions);

        Assert.Equal(ItemRarity.Exotic, rarity);
        Assert.Equal("\"Exotic\"", JsonSerializer.Serialize(rarity, StringStructOptions));
    }
}
