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
    public void EffectiveCost_OfAVendorAcquirableLeafWithoutABuyOrder_IsZero_NotUnknown()
    {
        // Untradeable but obtainable from a vendor for some (non-coin) currency, e.g. an Obsidian Shard for
        // karma: obtainable, just not coin-priced, so it reads as 0 coin rather than unknown.
        RecipeNode leaf = Leaf(buy: 0);
        leaf.IsVendorAcquirable = true;

        Assert.Equal(0m, leaf.EffectiveCostOrNull);
    }

    [Fact]
    public void EffectiveCost_StaysKnown_WhenAnUnpricedIngredientIsVendorAcquirable()
    {
        // A craftable whose only un-coin-priced ingredient is vendor-acquirable is still fully known: the craft
        // sum (10 + 0) wins over the buy order, instead of falling back to it.
        RecipeNode vendorChild = Leaf(buy: 0);
        vendorChild.IsVendorAcquirable = true;
        RecipeNode root = Craftable(buy: 40, count: 1, Leaf(buy: 10), vendorChild);

        Assert.Equal(10m, root.EffectiveCostOrNull);
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
    public void EffectiveCost_KeepsThePricedParts_WhenAnUntradeableIngredientHasNoCost()
    {
        // An untradeable craftable (no buy order) with one priced ingredient and one untradeable leaf: the
        // roll-up counts the untradeable part as 0 and keeps the priced part, instead of collapsing to 0.
        RecipeNode root = Craftable(buy: 0, count: 1, Leaf(buy: 10), Leaf(buy: 0));

        Assert.Equal(10m, root.EffectiveCost); // 10 (priced) + 0 (untradeable)
        Assert.Null(root.EffectiveCostOrNull); // still "not fully known" — that signal is unchanged
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
    public void CraftCostKnown_IsFalse_WhenAnIngredientIsGenuinelyUnobtainable()
    {
        // Dusk's case: a craftable whose ingredient has no buy order, no recipe, and no vendor (any currency) —
        // its craft cost can't be known, so the parent's isn't either (it shows an em-dash, not a deflated sum).
        RecipeNode root = Craftable(buy: 0, count: 1, Leaf(buy: 10), Leaf(buy: 0));

        Assert.False(root.CraftCostKnown);
    }

    [Fact]
    public void CraftCostKnown_IsTrue_WhenAnUnpricedIngredientIsVendorAcquirable()
    {
        // The same shape but the unpriced ingredient is vendor-acquirable (some currency): obtainable, so the
        // craft cost is considered known.
        RecipeNode vendorChild = Leaf(buy: 0);
        vendorChild.IsVendorAcquirable = true;
        RecipeNode root = Craftable(buy: 0, count: 1, Leaf(buy: 10), vendorChild);

        Assert.True(root.CraftCostKnown);
    }

    [Fact]
    public void CraftCostKnown_IsFalse_ForALeaf()
    {
        Assert.False(Leaf(buy: 10).CraftCostKnown);
    }

    [Fact]
    public void IsPurchasable_TrueWithABuyOrderOrVendor_FalseWhenAccountBound()
    {
        Assert.True(Leaf(buy: 10).IsPurchasable);   // trading-post offer

        RecipeNode vendor = Leaf(buy: 0);
        vendor.IsVendorAcquirable = true;
        Assert.True(vendor.IsPurchasable);           // sold by a vendor (any currency)

        Assert.False(Leaf(buy: 0).IsPurchasable);    // no offer, no vendor → unpurchasable (em-dash)
    }

    [Fact]
    public void ShowCraftCost_IsFalse_AndCollapsesToBuy_WhenADirectIngredientIsAnUnobtainableDeadEnd()
    {
        // Dusk's case: a craftable with a buy order whose direct ingredient is a dead-end (no buy, no recipe, no
        // vendor — Spirit/Essence). The craft can't be performed, so it collapses to buy: craft shown as em-dash,
        // buy bolded as the value.
        RecipeNode dusk = Craftable(buy: 160, count: 1, Leaf(buy: 0), Leaf(buy: 50));

        Assert.False(dusk.ShowCraftCost);
        Assert.True(dusk.BuyIsCheaper);
        Assert.False(dusk.CraftIsCheaper);
    }

    [Fact]
    public void ShowCraftCost_IsTrue_WhenTheDeadEndIsBuriedBelowACraftableIngredient()
    {
        // Twilight's case: its direct ingredients are all craftable (a gift with a buried dead-end, plus a priced
        // leaf) — no *direct* dead-end — so it keeps its craft estimate instead of collapsing.
        RecipeNode giftWithBuriedDeadEnd = Craftable(buy: 0, count: 1, Leaf(buy: 0));
        RecipeNode twilight = Craftable(buy: 2085, count: 1, giftWithBuriedDeadEnd, Leaf(buy: 100));

        Assert.False(giftWithBuriedDeadEnd.ShowCraftCost); // it has a direct dead-end
        Assert.True(twilight.ShowCraftCost);               // its direct ingredients are all obtainable/craftable
    }

    [Fact]
    public void ShowCraftCost_IsTrue_WhenAnUnpricedDirectIngredientIsVendorAcquirable()
    {
        // A vendor-acquirable leaf (some currency) is obtainable, not a dead-end, so it doesn't force a collapse.
        RecipeNode vendorLeaf = Leaf(buy: 0);
        vendorLeaf.IsVendorAcquirable = true;
        RecipeNode node = Craftable(buy: 50, count: 1, vendorLeaf, Leaf(buy: 10));

        Assert.True(node.ShowCraftCost);
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
