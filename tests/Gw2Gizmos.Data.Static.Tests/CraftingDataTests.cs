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

        // A forge recipe merged in, fully resolved to item ids (what the engine needs).
        Assert.True(StaticRecipes.ByOutputItemId.TryGetValue(MysticClover, out StaticRecipe? clover));
        Assert.NotEmpty(clover!.Ingredients);
        Assert.All(clover.Ingredients, ingredient => Assert.True(ingredient.ItemId > 0));
    }
}
