using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// What goals still need: holdings set aside from the top of each tree, the rest made or bought along the cheapest
/// path, and goals sharing the holdings in order.
/// </summary>
public sealed class GoalPlannerTests
{
    private static readonly Dictionary<int, long> NoWallet = new();

    // An item the trading post sells for `price` each, `count` of them.
    private static RecipeNode Bought(int id, long count, int price) =>
        new() { ItemId = id, ItemName = $"Item {id}", Count = count, BuyPricePerUnit = price };

    // An item made from `ingredients` (counts for `count` of it), that the trading post sells for `price` each, or
    // not at all when 0.
    private static RecipeNode Made(int id, long count, int price, params RecipeNode[] ingredients) =>
        new()
        {
            ItemId = id, ItemName = $"Item {id}", Count = count, BuyPricePerUnit = price,
            Ingredients = ingredients.ToList(),
        };

    // A part earned by playing, that nothing sells.
    private static RecipeNode Earned(int id, long count, string via) =>
        new() { ItemId = id, ItemName = $"Item {id}", Count = count, AcquiredVia = via, IsEarned = true };

    private static Goal Goal(int itemId, int count = 1) => new() { AccountId = "acc", ItemId = itemId, Count = count };

    private static GoalPlan Plan(Dictionary<int, long> held, params (Goal, RecipeNode)[] goals) =>
        GoalPlanner.Plan(goals, held, NoWallet);

    // A legendary (1) made from a precursor (2, itself made from 3) and 250 of a material (4).
    private static RecipeNode Legendary() =>
        Made(1, 1, 0, Made(2, 1, 500_000, Bought(3, 100, 10_000)), Bought(4, 250, 2_000));

    [Fact]
    public void A_held_part_covers_its_whole_branch_and_a_partial_stack_covers_what_it_can()
    {
        GoalPlan plan = Plan(new() { [2] = 1, [3] = 100, [4] = 120 }, (Goal(1), Legendary()));

        GoalNeeds needs = Assert.Single(plan.Goals);
        Assert.Equal([(2, 1L), (4, 120L)], needs.Covered.Select(l => (l.ItemId, l.Count)).OrderBy(p => p.ItemId));
        GoalLine buy = Assert.Single(needs.ToBuy);
        Assert.Equal((4, 130L, 260_000L), (buy.ItemId, buy.Count, buy.Copper));
        // The precursor's own material isn't needed, so it isn't set aside.
        Assert.Equal(new Dictionary<int, long> { [2] = 1, [4] = 120 }, plan.Reserved);
    }

    [Fact]
    public void Goals_share_holdings_in_order()
    {
        GoalPlan plan = Plan(new() { [4] = 300 }, (Goal(1), Legendary()), (Goal(1), Legendary()));

        Assert.Equal(250, plan.Goals[0].Covered.Single(l => l.ItemId == 4).Count);
        Assert.Equal(50, plan.Goals[1].Covered.Single(l => l.ItemId == 4).Count);
        Assert.Equal(200, plan.Goals[1].ToBuy.Single(l => l.ItemId == 4).Count);
        Assert.Equal(300, plan.Reserved[4]);
    }

    [Fact]
    public void A_part_cheaper_to_buy_is_bought_and_what_its_made_from_is_left_alone()
    {
        // Item 5 costs 1,000 to buy, and 2 × 800 to make from item 6.
        RecipeNode tree = Made(5, 1, 1_000, Bought(6, 2, 800));

        GoalPlan plan = Plan(new() { [6] = 2 }, (Goal(5), tree));

        GoalNeeds needs = Assert.Single(plan.Goals);
        Assert.Equal((5, 1L, 1_000L), needs.ToBuy.Select(l => (l.ItemId, l.Count, l.Copper)).Single());
        Assert.Empty(needs.Covered);
        Assert.Empty(plan.Reserved);
    }

    [Fact]
    public void Parts_that_cant_be_bought_are_listed_with_how_theyre_had()
    {
        // Item 7, on the trading post for far more than its buyable part, also takes 3 parts earned by playing.
        RecipeNode tree = Made(7, 1, 900_000, Bought(8, 10, 100), Earned(9, 3, "world completion"));

        GoalNeeds needs = Assert.Single(Plan(new() { [9] = 1 }, (Goal(7), tree)).Goals);

        GoalLine bound = Assert.Single(needs.CantBuy);
        Assert.Equal((9, 2L, "world completion"), (bound.ItemId, bound.Count, bound.Note));
        Assert.Equal(1_000, needs.BuyCopper);
        Assert.False(needs.Done);
    }

    [Fact]
    public void A_precursor_made_from_bound_parts_is_made_rather_than_bought()
    {
        // The precursor (14) lists for 3,100,000, and is made from a gift (15, 400,000 to buy) and a bound part (16)
        // nothing sells.
        RecipeNode tree = Made(13, 1, 0, Made(14, 1, 3_100_000, Bought(15, 1, 400_000),
            new RecipeNode { ItemId = 16, ItemName = "Item 16", Count = 1 }));

        GoalNeeds needs = Assert.Single(Plan(new(), (Goal(13), tree)).Goals);

        Assert.Equal(15, Assert.Single(needs.ToBuy).ItemId);
        Assert.Equal(16, Assert.Single(needs.CantBuy).ItemId);
    }

    [Fact]
    public void A_count_scales_the_tree_after_held_copies_of_the_goal()
    {
        // 3 of item 10, each made from 4 of item 11; one is held.
        RecipeNode tree = Made(10, 1, 0, Bought(11, 4, 50));

        GoalNeeds needs = Assert.Single(Plan(new() { [10] = 1 }, (Goal(10, count: 3), tree)).Goals);

        Assert.Equal((11, 8L, 400L), needs.ToBuy.Select(l => (l.ItemId, l.Count, l.Copper)).Single());
        Assert.Equal(1.0 / 3, needs.Progress, 6);
    }

    [Fact]
    public void A_vendor_purchase_spends_currencies_from_the_wallet_in_goal_order()
    {
        // Item 12 is only sold by a vendor: 5 for 1,000 karma (currency 2) and 10 copper.
        RecipeNode Tree() => new()
        {
            ItemId = 12, ItemName = "Item 12", Count = 1, IsVendorAcquirable = true,
            // Karma here is also a (made-up) legacy item, 99, as Spirit Shards and Research Notes are: it's spent
            // from the wallet all the same.
            VendorOffers = [new VendorOffer(5, [new VendorCost(1_000, "Karma", 99, "karma.png", CurrencyId: 2),
                new VendorCost(10, "Coin", null, null, IsCoin: true)])],
        };

        GoalPlan plan = GoalPlanner.Plan([(Goal(12, count: 5), Tree()), (Goal(12, count: 6), Tree())],
            new Dictionary<int, long>(), new Dictionary<int, long> { [2] = 2_500 });

        GoalCurrency first = Assert.Single(plan.Goals[0].Currencies);
        Assert.Equal((1_000L, 1_000L), (first.Needed, first.Have));
        GoalCurrency second = Assert.Single(plan.Goals[1].Currencies);
        Assert.Equal((2_000L, 1_500L, 500L), (second.Needed, second.Have, second.Missing));
        Assert.Equal(20, plan.Goals[1].FromVendors.Single().Copper);
    }

    [Fact]
    public void A_goal_held_outright_is_done()
    {
        GoalNeeds needs = Assert.Single(Plan(new() { [1] = 1 }, (Goal(1), Legendary())).Goals);

        Assert.True(needs.Done);
        Assert.Equal(1, needs.Progress);
    }
}
