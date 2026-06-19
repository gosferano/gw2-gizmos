using System.Collections.Concurrent;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.Static.Crafting;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.RecipeFinder;

public class RecipeTreeBuilder
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Configuration _priceConfiguration;

    public RecipeTreeBuilder(Gw2GizmosDbContext dbContext, Configuration priceConfiguration)
    {
        _dbContext = dbContext;
        _priceConfiguration = priceConfiguration;
    }

    public async Task<List<RecipeNode>> GetRecipeTrees(CancellationToken stoppingToken)
    {
        var allRecipes = await _dbContext.Recipes.Include(r => r.Ingredients).ToArrayAsync(stoppingToken);
        var recipeTrees = new List<RecipeNode>();

        for (var i = 0; i < allRecipes.Length; i++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            // Report progress every 100 recipes
            if (i % 100 == 0)
            {
                Console.WriteLine($"Processing recipe {i + 1}/{allRecipes.Length}");
            }

            RecipeNode rootNode = await BuildTreeAsync(allRecipes[i].OutputItemId, stoppingToken);

            recipeTrees.Add(rootNode);
        }

        return recipeTrees;
    }

    public List<RecipeNode> GetMostProfitableRecipesAsync(List<RecipeNode> recipes, int topCount)
    {
        // Sort by profit margin (SellPriceAfterFee - CraftingCostWithFee) and take the top results
        return recipes
            .OrderByDescending(node => (node.SellPrice * 0.85m) - (node.CraftingCost))
            .Take(topCount)
            .ToList();
    }

    public List<RecipeNode> GetMostProfitablePercentageRecipesAsync(List<RecipeNode> recipes, int topCount)
    {
        // Sort by profitability percentage and take the top results
        return recipes
            .OrderByDescending(node => node.IsProfitable ? (node.SellPrice * 0.85m) / node.CraftingCost : 0)
            .Take(topCount)
            .ToList();
    }

    // Safety backstop against a cycle in the recipe data (e.g. a Mystic Forge promotion loop): GW2 craft trees
    // are only a handful deep, so past this depth we stop expanding and price the node as a leaf rather than
    // recursing forever.
    private const int MaxRecipeDepth = 40;

    public async Task<RecipeNode> BuildTreeAsync(int rootItemId, CancellationToken ct, int parentMultiplier = 1, int depth = 0)
    {
        var rootNode = new RecipeNode { ItemId = rootItemId, Count = parentMultiplier };
        var stack = new Stack<(RecipeNode Node, bool Processed)>();
        stack.Push((rootNode, false));

        while (stack.Count > 0)
        {
            var (currentNode, processed) = stack.Pop();

            if (processed)
            {
                // Calculate crafting cost per unit after processing all children
                if (currentNode.Ingredients.Count > 0)
                {
                    currentNode.CraftingCostPerUnit =
                        currentNode.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost)
                        / currentNode.Count;
                }

                _memoizationCache.TryAdd(currentNode.ItemId, CopyForMemo(currentNode, 1));
                continue;
            }

            // CHECK CACHE FIRST - before any expensive operations
            if (_memoizationCache.TryGetValue(currentNode.ItemId, out var cachedNode))
            {
                var scaledNode = CopyForMemo(cachedNode, currentNode.Count);

                currentNode.ItemName = scaledNode.ItemName;
                currentNode.SellPricePerUnit = scaledNode.SellPricePerUnit;
                currentNode.BuyPricePerUnit = scaledNode.BuyPricePerUnit;
                currentNode.CraftingCostPerUnit = scaledNode.CraftingCostPerUnit;
                currentNode.OutputItemCount = scaledNode.OutputItemCount;
                currentNode.Ingredients = scaledNode.Ingredients;
                continue;
            }

            // Fetch prices
            TradingPostPrices tradingPostPrices = await GetPricesAsync(currentNode.ItemId, ct);
            currentNode.BuyPricePerUnit =
                _priceConfiguration.BuyPriceType == PriceType.BuyOrder
                    ? tradingPostPrices.BuyOrderPrice
                    : tradingPostPrices.SellOrderPrice;
            currentNode.SellPricePerUnit =
                _priceConfiguration.SellPriceType == PriceType.BuyOrder
                    ? tradingPostPrices.BuyOrderPrice
                    : tradingPostPrices.SellOrderPrice;

            // Fetch item name with fallback
            currentNode.ItemName = await GetItemNameAsync(currentNode.ItemId, ct);

            // Too deep (almost certainly a recipe-data cycle) — stop expanding and treat as a priced leaf.
            if (depth >= MaxRecipeDepth)
            {
                stack.Push((currentNode, true));
                continue;
            }

            // Fetch ALL recipes for this item
            var recipes = await _dbContext
                .Recipes.Include(r => r.Ingredients)
                .Where(r => r.OutputItemId == currentNode.ItemId)
                .ToListAsync(ct);

            if (recipes is { Count: > 0 })
            {
                RecipeNode? bestRecipeTree = null;
                var lowestCraftingCost = decimal.MaxValue;

                foreach (Recipe recipe in recipes)
                {
                    // Build a separate tree for each recipe
                    RecipeNode recipeTree = await BuildRecipeTreeForComparison(recipe, currentNode.Count, ct, depth);

                    if (recipeTree.CraftingCostPerUnit < lowestCraftingCost)
                    {
                        lowestCraftingCost = recipeTree.CraftingCostPerUnit;
                        bestRecipeTree = recipeTree;
                    }
                }

                if (bestRecipeTree != null)
                {
                    // Copy the best recipe's data to current node
                    currentNode.OutputItemCount = bestRecipeTree.OutputItemCount;
                    currentNode.CraftingCostPerUnit = bestRecipeTree.CraftingCostPerUnit;
                    currentNode.Ingredients = bestRecipeTree.Ingredients;

                    // Mark as processed since we've built the complete subtree
                    _memoizationCache.TryAdd(currentNode.ItemId, CopyForMemo(currentNode, 1));
                }
            }
            else if (StaticRecipes.ByOutputItemId.TryGetValue(currentNode.ItemId, out StaticRecipe? staticRecipe))
            {
                // No API recipe, but a hardcoded one (Mystic Forge / daily craft) — price it from its inputs
                // so an otherwise account-bound intermediate isn't a 0-cost leaf.
                RecipeNode recipeTree = await BuildStaticRecipeTree(staticRecipe, currentNode.Count, ct, depth);
                currentNode.OutputItemCount = recipeTree.OutputItemCount;
                currentNode.CraftingCostPerUnit = recipeTree.CraftingCostPerUnit;
                currentNode.Ingredients = recipeTree.Ingredients;
                _memoizationCache.TryAdd(currentNode.ItemId, CopyForMemo(currentNode, 1));
            }
            else
            {
                // Leaf node - no recipe, mark as processed
                stack.Push((currentNode, true));
            }
        }

        return rootNode;
    }

    private readonly ConcurrentDictionary<int, RecipeNode> _memoizationCache = new();

    private RecipeNode CopyForMemo(RecipeNode node, int targetCount)
    {
        // Calculate crafts needed for both cached node and target
        int cachedCraftsNeeded = (node.Count + node.OutputItemCount - 1) / node.OutputItemCount;
        int targetCraftsNeeded = (targetCount + node.OutputItemCount - 1) / node.OutputItemCount;
        double scalingFactor = (double)targetCraftsNeeded / cachedCraftsNeeded;

        return new RecipeNode
        {
            ItemId = node.ItemId,
            ItemName = node.ItemName,
            SellPricePerUnit = node.SellPricePerUnit,
            BuyPricePerUnit = node.BuyPricePerUnit,
            CraftingCostPerUnit = node.CraftingCostPerUnit,
            Count = targetCount,
            OutputItemCount = node.OutputItemCount,
            Ingredients = node
                .Ingredients.Select(ingredient =>
                {
                    var scaledCount = (int)Math.Round(ingredient.Count * scalingFactor);
                    return CopyForMemo(ingredient, scaledCount);
                })
                .ToList()
        };
    }

    private async Task<RecipeNode> BuildRecipeTreeForComparison(Recipe recipe, int targetCount, CancellationToken ct, int depth = 0)
    {
        var recipeNode = new RecipeNode
        {
            ItemId = recipe.OutputItemId,
            Count = targetCount,
            OutputItemCount = recipe.OutputItemCount
        };

        int recipeCraftsNeeded = (targetCount + recipe.OutputItemCount - 1) / recipe.OutputItemCount;

        // Build ingredient trees recursively
        foreach (var ingredient in recipe.Ingredients)
        {
            int requiredCount = ingredient.Count * recipeCraftsNeeded;
            if (requiredCount == 0)
                requiredCount = 1;

            RecipeNode ingredientTree;

            // Check if this ingredient is a currency
            if (ingredient.Type == "Currency")
            {
                ingredientTree = new RecipeNode
                {
                    ItemId = ingredient.Id,
                    Count = requiredCount,
                    IsCurrency = true,
                    BuyPricePerUnit = 0,
                    SellPricePerUnit = 0,
                    CraftingCostPerUnit = 0,
                    ItemName = await GetCurrencyNameAsync(ingredient.Id, ct)
                };
            }
            else
            {
                ingredientTree = await BuildTreeAsync(ingredient.Id, ct, requiredCount, depth + 1);
            }

            recipeNode.Ingredients.Add(ingredientTree);
        }

        // Calculate crafting cost
        recipeNode.CraftingCostPerUnit =
            recipeNode.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost) / targetCount;

        return recipeNode;
    }

    /// <summary>
    /// Builds a subtree for a hardcoded <see cref="StaticRecipe"/> (one the API doesn't expose). Mirrors
    /// <see cref="BuildRecipeTreeForComparison"/>, but every ingredient is a real item (no currency inputs),
    /// so each recurses through <see cref="BuildTreeAsync"/> and is priced from the trading post / vendors.
    /// </summary>
    private async Task<RecipeNode> BuildStaticRecipeTree(StaticRecipe recipe, int targetCount, CancellationToken ct, int depth = 0)
    {
        var recipeNode = new RecipeNode
        {
            ItemId = recipe.OutputItemId,
            Count = targetCount,
            OutputItemCount = recipe.OutputItemCount
        };

        int craftsNeeded = (targetCount + recipe.OutputItemCount - 1) / recipe.OutputItemCount;

        foreach (StaticIngredient ingredient in recipe.Ingredients)
        {
            int requiredCount = ingredient.Count * craftsNeeded;
            if (requiredCount == 0)
            {
                requiredCount = 1;
            }

            recipeNode.Ingredients.Add(await BuildTreeAsync(ingredient.ItemId, ct, requiredCount, depth + 1));
        }

        recipeNode.CraftingCostPerUnit =
            recipeNode.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost) / targetCount;

        return recipeNode;
    }

    /// <summary>The best trading-post buy/sell unit prices for an item, or 0 when it isn't listed.</summary>
    private async Task<TradingPostPrices> GetPricesAsync(int itemId, CancellationToken ct)
    {
        // Price from the latest price-poll snapshot, not the raw listing tables. One poll captures every
        // tradeable item at once, so a cold-start tree is fully priced after the first poll (~minutes)
        // instead of dribbling in as the far larger listings sync completes — and it's the same source the
        // Items grid and history chart use, so they all agree. An item with no snapshot (not on the trading
        // post) reads as 0; Buy/Sell can each be null when that side of the book is empty.
        var latest = await _dbContext
            .PriceSnapshots.Where(snapshot => snapshot.ItemId == itemId)
            .OrderByDescending(snapshot => snapshot.Id)
            .Select(snapshot => new { snapshot.Buy, snapshot.Sell })
            .FirstOrDefaultAsync(ct);

        // Acquisition price (SellOrderPrice = lowest sell listing = what you pay): take the cheaper of the
        // trading post and a coin vendor that sells it. The vendor price both fills in untradeable items the
        // TP doesn't list and undercuts the TP where a vendor is cheaper. BuyOrderPrice stays TP-only.
        int tradingPostSell = latest?.Sell ?? 0;
        int? vendor = VendorItems.CopperPriceFor(itemId);
        int sellOrderPrice = (tradingPostSell, vendor) switch
        {
            ( > 0, int v) => Math.Min(tradingPostSell, v),
            (0, int v) => v,
            _ => tradingPostSell
        };

        return new TradingPostPrices(sellOrderPrice, latest?.Buy ?? 0);
    }

    private async Task<string> GetItemNameAsync(int itemId, CancellationToken ct)
    {
        var item = await _dbContext.Items.FindAsync(new object[] { itemId }, ct);
        return item?.Name ?? $"Unknown Item ({itemId})";
    }

    private async Task<string> GetCurrencyNameAsync(int currencyId, CancellationToken ct)
    {
        var currency = await _dbContext.Currencies.FirstOrDefaultAsync(c => c.Id == currencyId, ct);
        return currency?.Name ?? $"Unknown Currency ({currencyId})";
    }
}
