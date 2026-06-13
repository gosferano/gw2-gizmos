using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.RecipeFinder.Tests;

public class RecipeNodeTests
{
    private static RecipeNode Leaf(int buy = 0, int sell = 0, int count = 1) =>
        new()
        {
            ItemId = 1,
            ItemName = "leaf",
            BuyPricePerUnit = buy,
            SellPricePerUnit = sell,
            Count = count,
        };

    private static RecipeNode Craftable(int buy, int count, params RecipeNode[] ingredients) =>
        new()
        {
            ItemId = 99,
            ItemName = "craftable",
            BuyPricePerUnit = buy,
            Count = count,
            Ingredients = ingredients.ToList(),
        };

    [Fact]
    public void Prices_ScaleByCount()
    {
        RecipeNode node = new()
        {
            ItemName = "x",
            SellPricePerUnit = 10,
            BuyPricePerUnit = 8,
            CraftingCostPerUnit = 5m,
            Count = 3,
        };

        Assert.Equal(30, node.SellPrice);
        Assert.Equal(24, node.BuyPrice);
        Assert.Equal(15m, node.CraftingCost);
        Assert.False(node.IsCraftable);
    }

    [Fact]
    public void EffectiveCost_OfALeaf_IsItsScaledBuyPrice()
    {
        RecipeNode leaf = Leaf(buy: 100, count: 2);

        Assert.Equal(200m, leaf.EffectiveCostOrNull);
        Assert.Equal(200m, leaf.EffectiveCost);
    }

    [Fact]
    public void EffectiveCost_OfALeafWithoutABuyOrder_IsUnknown()
    {
        RecipeNode leaf = Leaf(buy: 0);

        Assert.Null(leaf.EffectiveCostOrNull);
        Assert.Equal(0m, leaf.EffectiveCost); // unknown rolls up as 0 for the builder
    }

    [Fact]
    public void EffectiveCost_PrefersCrafting_WhenCheaperThanBuying()
    {
        RecipeNode root = Craftable(buy: 30, count: 1, Leaf(buy: 10), Leaf(buy: 10));

        Assert.Equal(20m, root.EffectiveCostOrNull);
    }

    [Fact]
    public void EffectiveCost_PrefersBuying_WhenCheaperThanCrafting()
    {
        RecipeNode root = Craftable(buy: 30, count: 1, Leaf(buy: 25), Leaf(buy: 25));

        Assert.Equal(30m, root.EffectiveCostOrNull);
    }

    [Fact]
    public void EffectiveCost_IgnoresCurrencyIngredients_InTheCraftSum()
    {
        RecipeNode currency = new() { ItemName = "coin", IsCurrency = true, Count = 50 };
        RecipeNode root = Craftable(buy: 0, count: 1, Leaf(buy: 10), currency);

        // Currency is free here; craft cost is just the real ingredient, and there's no buy order.
        Assert.Equal(10m, root.EffectiveCostOrNull);
    }

    [Fact]
    public void EffectiveCost_FallsBackToBuy_WhenAnIngredientCostIsUnknown()
    {
        // One ingredient has no buy order and no recipe → craft cost is unknown.
        RecipeNode root = Craftable(buy: 40, count: 1, Leaf(buy: 10), Leaf(buy: 0));

        Assert.Equal(40m, root.EffectiveCostOrNull); // craft unknown → use the buy order
        Assert.False(root.CraftCostKnown);
    }

    [Fact]
    public void EffectiveCost_IsUnknown_WhenNeitherCraftNorBuyIsKnown()
    {
        RecipeNode root = Craftable(buy: 0, count: 1, Leaf(buy: 0));

        Assert.Null(root.EffectiveCostOrNull);
        Assert.False(root.CraftCostKnown);
    }

    [Fact]
    public void EffectiveCost_RollsUpThroughNestedCraftables()
    {
        // sub = craft(leaf 5 + leaf 5) = 10, no buy → effective 10
        RecipeNode sub = Craftable(buy: 0, count: 1, Leaf(buy: 5), Leaf(buy: 5));
        // root = craft(sub 10 + leaf 3) = 13, no buy
        RecipeNode root = Craftable(buy: 0, count: 1, sub, Leaf(buy: 3));

        Assert.Equal(13m, root.EffectiveCostOrNull);
        Assert.True(root.CraftCostKnown);
    }

    [Fact]
    public void CraftCostKnown_IsFalse_ForALeaf()
    {
        Assert.False(Leaf(buy: 10).CraftCostKnown);
    }

    [Theory]
    [InlineData(5, 10, 12, true)] // craftable below the buy order → profitable
    [InlineData(15, 10, 12, false)] // costs more than the buy order, and it sells → not profitable
    [InlineData(5, 0, 0, true)] // no sell price known → treated as profitable to surface it
    [InlineData(0, 10, 12, false)] // zero craft cost → never profitable
    public void IsProfitable_FollowsTheCostVsBuyOrderRule(
        int craftPerUnit,
        int buyPerUnit,
        int sellPerUnit,
        bool expected
    )
    {
        RecipeNode node = new()
        {
            ItemName = "x",
            CraftingCostPerUnit = craftPerUnit,
            BuyPricePerUnit = buyPerUnit,
            SellPricePerUnit = sellPerUnit,
            Count = 1,
        };

        Assert.Equal(expected, node.IsProfitable);
    }
}
