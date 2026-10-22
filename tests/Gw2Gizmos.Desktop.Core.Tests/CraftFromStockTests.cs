using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// What's worth crafting from owned materials: an owned material costs what it would sell for (or, if it can't be
/// sold, what crafting another costs), a missing one its lowest listing, and the result sells at its highest buy order
/// less the trading post's 15%.
/// </summary>
public sealed class CraftFromStockTests
{
    private const int Output = 1;
    private const int Ore = 10;
    private const int Gem = 11;
    private const int Lump = 12;

    private readonly Dictionary<int, int> _held = [];
    private readonly Dictionary<int, long> _values = [];
    private readonly Dictionary<int, long> _craftCosts = [];
    private readonly Dictionary<int, (int Buy, int Sell)> _prices = [];
    private readonly Dictionary<string, int> _ratings = new() { ["Jeweler"] = 400 };
    private HashSet<int>? _learned;

    private static StockRecipe Recipe(int minRating = 400, params (int ItemId, int Count)[] ingredients) =>
        new(1, Output, 1, minRating, ["Jeweler"], false, false, ingredients);

    private List<StockCraft> Find(StockRecipe recipe) =>
        CraftFromStock.Find([recipe], _held, _values, _craftCosts, _prices, _ratings, _learned);

    [Fact]
    public void A_craft_from_stock_makes_its_result_less_what_its_materials_would_sell_for()
    {
        _held[Ore] = 25;
        _values[Ore] = 10;
        _prices[Output] = (Buy: 1_000, Sell: 1_200);

        StockCraft craft = Assert.Single(Find(Recipe(ingredients: (Ore, 10))));

        Assert.Equal(850, craft.OutputCopper); // 1000 less 15%
        Assert.Equal(100, craft.UsesCopper);
        Assert.Equal(750, craft.ProfitCopper);
        Assert.Equal(2, craft.CanMake);
        Assert.Equal(1_500, craft.StockProfitCopper);
        Assert.Empty(craft.Missing);
    }

    [Fact]
    public void A_missing_material_is_bought_at_its_lowest_listing()
    {
        _held[Ore] = 10;
        _values[Ore] = 50;
        _prices[Gem] = (Buy: 80, Sell: 100);
        _prices[Output] = (Buy: 2_000, Sell: 2_500);

        StockCraft craft = Assert.Single(Find(Recipe(ingredients: [(Ore, 10), (Gem, 2)])));

        StockIngredient missing = Assert.Single(craft.Missing);
        Assert.Equal((Gem, 2, 200L), (missing.ItemId, missing.Count, missing.Copper));
        Assert.Equal(1_700 - 500 - 200, craft.ProfitCopper);
        Assert.Equal(0, craft.CanMake);
    }

    [Fact]
    public void A_craft_mostly_bought_is_left_out()
    {
        _held[Ore] = 10;
        _values[Ore] = 1;
        _prices[Gem] = (Buy: 80, Sell: 100);
        _prices[Output] = (Buy: 10_000, Sell: 12_000);

        Assert.Empty(Find(Recipe(ingredients: [(Ore, 10), (Gem, 2)])));
    }

    [Fact]
    public void A_craft_short_of_something_unbuyable_is_left_out()
    {
        _held[Ore] = 10;
        _values[Ore] = 50;
        _prices[Output] = (Buy: 10_000, Sell: 12_000);

        Assert.Empty(Find(Recipe(ingredients: [(Ore, 10), (Lump, 1)])));
    }

    [Fact]
    public void An_owned_material_that_cant_be_sold_costs_what_crafting_another_does()
    {
        _held[Ore] = 10;
        _values[Ore] = 50;
        _held[Lump] = 1;
        _craftCosts[Lump] = 300;
        _prices[Output] = (Buy: 2_000, Sell: 2_500);

        StockCraft craft = Assert.Single(Find(Recipe(ingredients: [(Ore, 10), (Lump, 1)])));

        StockIngredient lump = Assert.Single(craft.Uses, u => u.ItemId == Lump);
        Assert.True(lump.AtCraftCost);
        Assert.Equal(1_700 - 500 - 300, craft.ProfitCopper);
    }

    [Fact]
    public void A_result_without_a_buy_order_is_left_out()
    {
        _held[Ore] = 10;
        _values[Ore] = 1;

        Assert.Empty(Find(Recipe(ingredients: (Ore, 10))));
    }

    [Fact]
    public void A_craft_worth_less_than_its_materials_is_left_out()
    {
        _held[Ore] = 10;
        _values[Ore] = 100;
        _prices[Output] = (Buy: 1_000, Sell: 1_200);

        Assert.Empty(Find(Recipe(ingredients: (Ore, 10))));
    }

    [Theory]
    [InlineData(400, true)]
    [InlineData(450, false)]
    public void A_craft_says_whether_a_character_has_the_level(int minRating, bool canCraft)
    {
        _held[Ore] = 10;
        _values[Ore] = 1;
        _prices[Output] = (Buy: 1_000, Sell: 1_200);

        Assert.Equal(canCraft, Assert.Single(Find(Recipe(minRating, (Ore, 10)))).CharactersCanCraft);
    }

    [Theory]
    [InlineData(new[] { 1 }, false, true)] // learned
    [InlineData(new int[0], false, false)] // not learned
    [InlineData(new int[0], true, true)] // learned at the discipline's level, which a character has
    public void A_craft_says_whether_the_account_knows_its_recipe(int[] learned, bool autoLearned, bool known)
    {
        _held[Ore] = 10;
        _values[Ore] = 1;
        _prices[Output] = (Buy: 1_000, Sell: 1_200);
        _learned = [.. learned];
        StockRecipe recipe = Recipe(ingredients: (Ore, 10)) with { AutoLearned = autoLearned };

        Assert.Equal(known, Assert.Single(Find(recipe)).Known);
    }

    [Fact]
    public void Without_the_learned_recipes_nothing_is_said_about_knowing_one()
    {
        _held[Ore] = 10;
        _values[Ore] = 1;
        _prices[Output] = (Buy: 1_000, Sell: 1_200);

        Assert.Null(Assert.Single(Find(Recipe(ingredients: (Ore, 10)))).Known);
    }
}
