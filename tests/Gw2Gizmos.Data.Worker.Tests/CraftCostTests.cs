using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    private const int MemoryOfSunrise = 95081;

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

    [Fact]
    public async Task A_memory_from_the_legendary_armory_costs_what_its_legendary_does()
    {
        Seed((Sunrise, 20_000_000));

        RecipeNode memory = await TreeOf(MemoryOfSunrise);

        Assert.Equal("the legendary armory", memory.AcquiredVia);
        Assert.Equal(Sunrise, Assert.Single(memory.Ingredients).ItemId);
        Assert.Equal(20_000_000m, memory.EffectiveCostOrNull);
    }

    [Fact]
    public async Task The_cached_craft_cost_is_left_out_when_a_direct_ingredient_cannot_be_acquired()
    {
        // Made-up ids no vendor sells: 9_000_001 and 9_000_002 are crafted from a priced material, the second also
        // from one that can't be acquired (no price, recipe or vendor).
        const int Priced = 9_000_010, Unobtainable = 9_000_011;
        Seed((Priced, 500));
        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            db.Recipes.AddRange(
                CraftOf(9_000_001, (Priced, 2)),
                CraftOf(9_000_002, (Priced, 2), (Unobtainable, 1))
            );
            db.SaveChanges();
        }

        await using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            var updater = new ItemCraftCostUpdater(db, NullLogger<ItemCraftCostUpdater>.Instance);
            await updater.UpdateCraftCosts(CancellationToken.None);
        }

        Dictionary<int, double> cached = _fixture
            .NewContext()
            .ItemCraftCosts.AsNoTracking()
            .ToDictionary(cost => cost.ItemId, cost => cost.CraftingCost);
        Assert.Equal(1_000, cached[9_000_001]);
        Assert.False(cached.ContainsKey(9_000_002));
    }

    private static Recipe CraftOf(int output, params (int ItemId, int Count)[] ingredients) =>
        new()
        {
            Type = "Refinement",
            OutputItemId = output,
            OutputItemCount = 1,
            ChatLink = "",
            Ingredients = ingredients
                .Select(ingredient =>
                    new RecipeIngredient { Id = ingredient.ItemId, Count = ingredient.Count, Type = "Item" }
                )
                .ToList(),
        };
}
