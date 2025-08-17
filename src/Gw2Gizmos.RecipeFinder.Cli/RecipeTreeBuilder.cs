using System.Collections.Concurrent;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

namespace Gw2Gizmos.RecipeFinder.Cli;

public class RecipeTreeBuilder
{
    private readonly IRecipeService _recipeService;
    private readonly IPriceService _priceService;
    private readonly IItemService _itemService;

    public RecipeTreeBuilder(IRecipeService recipeService, IItemService itemService, IPriceService priceService)
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

    private readonly ConcurrentDictionary<int, RecipeNode> _memoizationCache = new();

    public async Task<RecipeNode> BuildTreeAsync(int rootItemId, CancellationToken ct, int parentMultiplier = 1)
    {
        if (_memoizationCache.TryGetValue(rootItemId, out RecipeNode? cachedNode))
        {
            return CopyMemo(cachedNode, parentMultiplier);
        }

        var rootNode = new RecipeNode { ItemId = rootItemId, Count = parentMultiplier };
        var stack = new Stack<(RecipeNode Node, bool Processed)>();
        stack.Push((rootNode, false));

        while (stack.Count > 0)
        {
            (RecipeNode currentNode, bool processed) = stack.Pop();

            if (processed)
            {
                if (currentNode.Ingredients.Count > 0)
                {
                    currentNode.CraftingCostPerUnit =
                        currentNode.Ingredients.Sum(child =>
                            child.IsCraftable && child.CraftingCostPerUnit < child.BuyPricePerUnit
                                ? child.CraftingCost
                                : child.BuyPrice
                        ) / currentNode.Count;
                }

                // Add the processed node to the cache
                _memoizationCache[currentNode.ItemId] = currentNode;

                continue;
            }

            stack.Push((currentNode, true));

            TradingPostPrices tradingPostPrices = await _priceService.GetPricesAsync(currentNode.ItemId, ct);
            currentNode.BuyPricePerUnit = tradingPostPrices.SellOrderPrice;
            currentNode.SellPricePerUnit = tradingPostPrices.BuyOrderPrice;

            string itemName = await _itemService.GetItemNameAsync(currentNode.ItemId, ct);
            currentNode.ItemName = !string.IsNullOrWhiteSpace(itemName)
                ? itemName
                : $"Unknown Item {currentNode.ItemId}";

            Recipe? recipe = await _recipeService.GetRecipeAsync(currentNode.ItemId, ct);

            if (recipe != null)
            {
                foreach (RecipeIngredient ingredient in recipe.Ingredients)
                {
                    int childMultiplier = ingredient.Count * currentNode.Count;

                    if (_memoizationCache.TryGetValue(ingredient.Id, out var cachedChildNode))
                    {
                        // Use the cached child node
                        var childNode = CopyMemo(cachedChildNode, childMultiplier);
                        currentNode.Ingredients.Add(childNode);
                    }
                    else
                    {
                        var childNode = new RecipeNode { ItemId = ingredient.Id, Count = childMultiplier };
                        currentNode.Ingredients.Add(childNode);
                        stack.Push((childNode, false));
                    }
                }
            }
        }

        _memoizationCache[rootItemId] = rootNode;
        return rootNode;
    }

    private RecipeNode CopyMemo(RecipeNode memo, int multiplier)
    {
        return new RecipeNode
        {
            ItemId = memo.ItemId,
            ItemName = memo.ItemName,
            SellPricePerUnit = memo.SellPricePerUnit,
            BuyPricePerUnit = memo.BuyPricePerUnit,
            CraftingCostPerUnit = memo.CraftingCostPerUnit,
            Count = multiplier,
            Ingredients = memo.Ingredients.Select(i => CopyMemo(i, multiplier)).ToList()
        };
    }
}
