using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
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

    private async Task<RecipeNode> TreeOf(int itemId, string? accountId = null)
    {
        await using Gw2GizmosDbContext db = _fixture.NewContext();
        var builder = new RecipeTreeBuilder(db, RecipeFinder.Configuration.Default, accountId);
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
    public async Task Items_earned_by_playing_count_as_obtainable_for_no_coin()
    {
        const int GiftOfMastery = 19674, GiftOfExploration = 19677, GiftOfBattle = 19678;

        RecipeNode gift = await TreeOf(GiftOfMastery);

        RecipeNode exploration = gift.Ingredients.Single(ingredient => ingredient.ItemId == GiftOfExploration);
        RecipeNode battle = gift.Ingredients.Single(ingredient => ingredient.ItemId == GiftOfBattle);
        Assert.Equal("world completion", exploration.AcquiredVia);
        Assert.Equal("a WvW reward track", battle.AcquiredVia);
        Assert.Equal(0m, exploration.EffectiveCostOrNull);
        Assert.Equal(0m, battle.EffectiveCostOrNull);
    }

    // Made-up ids no vendor, recipe or trading post knows: a reward only an achievement gives, and its collection's
    // pieces.
    private const int Reward = 9_200_001, Piece = 9_200_010, CheapPiece = 9_200_011;

    private void SeedAchievement(int id, string name, int reward, params int[] pieces) =>
        SeedAchievement(id, name, "ItemSet", reward, pieces);

    private void SeedAchievement(int id, string name, string type, int reward, params int[] pieces)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        db.Achievements.Add(new Achievement
        {
            Id = id,
            Name = name,
            Type = type,
            Bits = pieces.Select(p => new AchievementBit { Type = "Item", Id = p })
                .Append(new AchievementBit { Type = "Skin", Id = 1 })
                .ToList(),
            Rewards = [new AchievementReward { Type = "Item", Id = reward, Count = 1 }],
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task An_item_only_an_achievement_gives_costs_its_collections_pieces()
    {
        Seed((Piece, 1_000), (CheapPiece, 300));
        SeedAchievement(1, "Collector", Reward, Piece, CheapPiece, Piece + 50);

        RecipeNode reward = await TreeOf(Reward);

        Assert.Equal("the Collector achievement", reward.AcquiredVia);
        Assert.Equal([Piece, CheapPiece, Piece + 50], reward.Ingredients.Select(i => i.ItemId));
        Assert.Equal(1_300m, reward.CraftingCost); // the unpriced third piece counts as 0, the skin not at all
        Assert.False(reward.CraftCostKnown);
    }

    [Fact]
    public async Task A_completed_collection_stays_a_source_labelled_completed()
    {
        Seed((Piece, 1_000));
        SeedAchievement(1, "Collector", Reward, Piece);
        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            db.AccountAchievements.Add(new AccountAchievement { AccountId = "acc", AchievementId = 1, Done = true });
            db.SaveChanges();
        }

        RecipeNode forOthers = await TreeOf(Reward);
        RecipeNode forTheAccount = await TreeOf(Reward, accountId: "acc");

        Assert.Equal(("the Collector achievement", 1_000m), (forOthers.AcquiredVia, forOthers.EffectiveCostOrNull));
        Assert.Equal(
            ("the Collector achievement, completed", 1_000m),
            (forTheAccount.AcquiredVia, forTheAccount.EffectiveCostOrNull)
        );
    }

    [Fact]
    public async Task An_item_with_another_way_to_get_it_is_never_priced_from_an_achievement()
    {
        Seed((Reward, 50), (Piece, 1_000));
        SeedAchievement(1, "Collector", Reward, Piece);

        RecipeNode reward = await TreeOf(Reward);

        Assert.Null(reward.AcquiredVia);
        Assert.Empty(reward.Ingredients);
        Assert.Equal(50m, reward.EffectiveCostOrNull);
    }

    [Fact]
    public async Task Only_a_collection_counts_as_a_source()
    {
        // A story achievement that hands out a gathered material (as Living World's do Pile of Silky Sand) isn't how
        // it's got.
        SeedAchievement(1, "Return to The Mystery Cave", "Default", Reward);

        RecipeNode reward = await TreeOf(Reward);

        Assert.Null(reward.AcquiredVia);
        Assert.Null(reward.EffectiveCostOrNull);
    }

    [Fact]
    public async Task A_collection_piece_the_API_does_not_list_has_its_shipped_name()
    {
        const int EirsAdvice = 76585; // Bolt I: The Experimental Sword's journal, not in /v2/items
        SeedAchievement(1, "Collector", Reward, EirsAdvice);

        RecipeNode reward = await TreeOf(Reward);

        Assert.Equal("Eir's Advice", Assert.Single(reward.Ingredients).ItemName);
    }

    [Fact]
    public async Task The_cheapest_achievement_giving_an_item_wins()
    {
        Seed((Piece, 1_000), (CheapPiece, 300));
        SeedAchievement(1, "Dear", Reward, Piece);
        SeedAchievement(2, "Cheap", Reward, CheapPiece);

        RecipeNode reward = await TreeOf(Reward);

        Assert.Equal(("the Cheap achievement", 300m), (reward.AcquiredVia, reward.CraftingCost));
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
