using System.Collections.Concurrent;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.RecipeFinder.Cli.Model;
using Gw2Gizmos.RecipeFinder.Cli.Services;

namespace Gw2Gizmos.RecipeFinder.Cli;

public class RecipeTreeBuilder
{
    private readonly RecipeService _recipeService;
    private readonly PriceService _priceService;
    private readonly ItemService _itemService;

    public RecipeTreeBuilder(RecipeService recipeService, ItemService itemService, PriceService priceService)
    {
        _recipeService = recipeService;
        _priceService = priceService;
        _itemService = itemService;
    }

    public async Task<List<RecipeNode>> GetRecipeTrees(CancellationToken stoppingToken)
    {
        var allRecipes = await _recipeService.GetAllRecipesAsync(stoppingToken);
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

    public async Task<RecipeNode> BuildTreeAsync(int rootItemId, CancellationToken ct, int parentMultiplier = 1)
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
                        currentNode.Ingredients.Sum(child =>
                            child.IsCraftable && child.CraftingCostPerUnit < child.BuyPricePerUnit
                                ? child.CraftingCost
                                : child.BuyPrice
                        ) / currentNode.Count;
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
            TradingPostPrices tradingPostPrices = await _priceService.GetPricesAsync(currentNode.ItemId, ct);
            currentNode.BuyPricePerUnit = tradingPostPrices.SellOrderPrice;
            currentNode.SellPricePerUnit = tradingPostPrices.BuyOrderPrice;

            // Fetch item name with fallback
            currentNode.ItemName = await _itemService.GetItemNameAsync(currentNode.ItemId, ct);

            // Fetch ALL recipes for this item
            var recipes = await _recipeService.GetRecipesAsync(currentNode.ItemId, ct);

            if (recipes is { Count: > 0 })
            {
                RecipeNode? bestRecipeTree = null;
                var lowestCraftingCost = decimal.MaxValue;

                foreach (Recipe recipe in recipes)
                {
                    // Build a separate tree for each recipe
                    RecipeNode recipeTree = await BuildRecipeTreeForComparison(recipe, currentNode.Count, ct);

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

    private async Task<RecipeNode> BuildRecipeTreeForComparison(Recipe recipe, int targetCount, CancellationToken ct)
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

            var ingredientTree = await BuildTreeAsync(ingredient.Id, ct, requiredCount);
            recipeNode.Ingredients.Add(ingredientTree);
        }

        // Calculate crafting cost
        recipeNode.CraftingCostPerUnit =
            recipeNode.Ingredients.Sum(child =>
                child.IsCraftable && child.CraftingCostPerUnit < child.BuyPricePerUnit
                    ? child.CraftingCost
                    : child.BuyPrice
            ) / targetCount;

        return recipeNode;
    }
}
