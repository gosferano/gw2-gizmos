using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Craft costs from the recipe engine over the embedded Mystic Forge recipes: which recipe variant prices an item,
/// and how items without a trading-post price are acquired.
/// </summary>
public sealed class CraftCostTests : IDisposable
{
    private const int Eternity = 30689;
    private const int Sunrise = 30703;
    private const int Twilight = 30704;
    private const int CrystallineDust = 24277;
    private const int PhilosophersStone = 20796;

    private readonly WorkerDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private void Seed(params (int ItemId, int Sell)[] prices)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        foreach ((int itemId, int sell) in prices)
        {
            db.Items.Add(
                new Item { Id = itemId, ChatLink = "", Name = $"Item {itemId}", Type = "Trophy", Rarity = "Basic" }
            );
            db.PriceSnapshots.Add(
                new PriceSnapshot { ItemId = itemId, TimestampUtc = DateTimeOffset.UtcNow, Buy = sell - 1, Sell = sell }
            );
        }

        db.SaveChanges();
    }

    private async Task<RecipeNode> TreeOf(int itemId)
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        var builder = new RecipeTreeBuilder(db, RecipeFinder.Configuration.Default);
        return await builder.BuildTreeAsync(itemId, CancellationToken.None);
    }

    [Fact]
    public async Task Eternity_is_priced_from_the_recipe_whose_ingredients_can_all_be_acquired()
    {
        // Eternity's other forge recipe takes Memory of Sunrise/Twilight, which have no price: counted as 0 it
        // looked cheapest (dust and stones only).
        Seed((Sunrise, 20_000_000), (Twilight, 25_000_000), (CrystallineDust, 300), (PhilosophersStone, 1_000));

        RecipeNode eternity = await TreeOf(Eternity);

        Assert.Contains(eternity.Ingredients, ingredient => ingredient.ItemId == Sunrise);
        Assert.Contains(eternity.Ingredients, ingredient => ingredient.ItemId == Twilight);
        Assert.True(eternity.CraftCostKnown);
        Assert.True(eternity.CraftingCost >= 45_000_000);
    }
}
