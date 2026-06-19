using Gw2Gizmos.Data.Static.Crafting;

namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>
/// Smoke tests for the embedded, gzipped crafting data (vendor catalog + Mystic Forge recipes). These guard the
/// load path specifically: a wrong resource name, a broken gzip, or the static-init-order trap (a JsonOptions
/// field read before it's set) all surface as silently-empty tables rather than an exception, so "non-empty +
/// a known lookup resolves" is the assertion that actually catches them.
/// </summary>
public class CraftingDataTests
{
    // Stable GW2 item ids used as fixtures.
    private const int BottleOfElonianWine = 19663; // sold by Miyani for coin
    private const int MysticClover = 19675;        // forged
    private const int ChargedQuartzCrystal = 43772; // hand-written Place-of-Power craft
    private const int MithrilOre = 19700;           // forge material promotion, random yield 40–200
    private const int ObsidianShard = 19925;        // only forge "recipe" sacrifices a minipet — nonsense

    [Fact]
    public void VendorItems_LoadFromEmbeddedCatalog()
    {
        Assert.NotEmpty(VendorItems.All);
        Assert.NotEmpty(VendorItems.ByItemId);

        // A coin-priced vendor item resolves to a positive copper floor.
        int? copper = VendorItems.CopperPriceFor(BottleOfElonianWine);
        Assert.NotNull(copper);
        Assert.True(copper > 0);
    }

    [Fact]
    public void VendorItems_KeepFullOfferDetail()
    {
        // Not just coin: an offer's cost components carry currency name + an item or currency id.
        VendorItem item = VendorItems.ByItemId[BottleOfElonianWine];
        Assert.NotEmpty(item.Offers);
        CostComponent component = item.Offers.SelectMany(o => o.Cost).First();
        Assert.False(string.IsNullOrEmpty(component.Currency));
        Assert.True(component.ItemId is not null || component.CurrencyId is not null);
    }

    [Fact]
    public void MysticForgeRecipes_LoadFromEmbeddedCatalog()
    {
        Assert.NotEmpty(MysticForgeRecipes.All);
        Assert.NotEmpty(MysticForgeRecipes.ByOutputItemId);

        Assert.True(MysticForgeRecipes.ByOutputItemId.TryGetValue(MysticClover, out var recipes));
        Assert.All(recipes!, recipe => Assert.NotEmpty(recipe.Ingredients));
    }

    [Fact]
    public void StaticRecipes_MergeHandWrittenAndForge()
    {
        Assert.NotEmpty(StaticRecipes.All);

        // The hand-written Place-of-Power craft the API lacks.
        Assert.True(StaticRecipes.ByOutputItemId.ContainsKey(ChargedQuartzCrystal));

        // Forge recipes merged in, fully resolved to item ids (what the engine needs).
        Assert.True(StaticRecipes.ByOutputItemId.TryGetValue(MysticClover, out IReadOnlyList<StaticRecipe>? clovers));
        Assert.NotEmpty(clovers!);
        Assert.All(clovers!, recipe =>
        {
            Assert.NotEmpty(recipe.Ingredients);
            Assert.All(recipe.Ingredients, ingredient => Assert.True(ingredient.ItemId > 0));
        });
    }

    [Fact]
    public void StaticRecipes_PromotionUsesAverageYield_AndDropsCircularRecipes()
    {
        // A material promotion is self-referential (it seeds the forge with some of the target tier as a
        // catalyst) and has a random yield. It is kept, but costed against the *average* output — Mithril Ore
        // yields 40–200, so 120 — not 1, which is what made it read as cheaply self-craftable before.
        Assert.True(StaticRecipes.ByOutputItemId.TryGetValue(MithrilOre, out IReadOnlyList<StaticRecipe>? mithril));
        StaticRecipe promotion = Assert.Single(mithril!);
        Assert.Equal(120m, promotion.OutputItemCount);
        Assert.Contains(promotion.Ingredients, ingredient => ingredient.ItemId == MithrilOre); // catalyst present

        // No curated recipe is net-circular: a self-referential recipe must yield strictly more than the
        // catalyst it consumes, otherwise its price is undefined (1-in-1-out recipes like Rurik's Engagement
        // Ring are dropped).
        foreach (StaticRecipe recipe in StaticRecipes.All)
        {
            int catalyst = recipe.Ingredients
                .Where(ingredient => ingredient.ItemId == recipe.OutputItemId)
                .Sum(ingredient => ingredient.Count);
            Assert.True(catalyst < recipe.OutputItemCount, $"Recipe for {recipe.OutputItemId} is net-circular");
        }

        // Obsidian Shard's only scraped forge recipe sacrifices a Mini Risen Priest of Balthazar — a minipet is
        // never a sensible input for a commodity material, so it's dropped and the item has no curated recipe.
        Assert.False(StaticRecipes.ByOutputItemId.ContainsKey(ObsidianShard));

        // Mystic Clover succeeds only ~30% of the time, so every variant's yield is chance-adjusted and kept
        // exact (not rounded): the 10-forge variant gives 10 × 0.3 = 3 (never the nominal 10). Both the 1- and
        // 10-forge variants are kept for the engine to compare.
        Assert.True(StaticRecipes.ByOutputItemId.TryGetValue(MysticClover, out IReadOnlyList<StaticRecipe>? clovers));
        Assert.Contains(clovers!, recipe => Math.Abs(recipe.OutputItemCount - 3m) < 0.05m);
        Assert.All(clovers!, recipe => Assert.True(recipe.OutputItemCount < 10m));
    }
}
