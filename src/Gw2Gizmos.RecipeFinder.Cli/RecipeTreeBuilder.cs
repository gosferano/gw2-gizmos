using System.Collections.Concurrent;

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

            // Fetch recipe
            var recipe = await _recipeService.GetRecipeAsync(currentNode.ItemId, ct);

            if (recipe != null)
            {
                currentNode.OutputItemCount = recipe.OutputItemCount;
                int recipeCraftsNeeded = (currentNode.Count + recipe.OutputItemCount - 1) / recipe.OutputItemCount;

                // Push this node as processed FIRST (it will be handled last due to stack LIFO)
                stack.Push((currentNode, true));

                // Then push all children (they will be processed first)
                foreach (var ingredient in recipe.Ingredients)
                {
                    int childMultiplier = ingredient.Count * recipeCraftsNeeded;
                    if (childMultiplier == 0)
                        childMultiplier = 1; // Prevent zero counts

                    var childNode = new RecipeNode { ItemId = ingredient.Id, Count = childMultiplier };
                    currentNode.Ingredients.Add(childNode);
                    stack.Push((childNode, false));
                }
            }
            else
            {
                // Leaf node - no recipe, mark as processed to calculate costs and cache
                stack.Push((currentNode, true));
            }
        }

        return rootNode;
    }

    private readonly ConcurrentDictionary<int, RecipeNode> _memoizationCache = new();

    private RecipeNode CopyForMemo(RecipeNode node, int targetCount)
    {
        // For recipes with OutputItemCount > 1, we need to scale based on actual crafts needed
        double scalingFactor;

        if (node.OutputItemCount > 1)
        {
            // Calculate crafts needed for both cached node and target
            int cachedCraftsNeeded = (node.Count + node.OutputItemCount - 1) / node.OutputItemCount;
            int targetCraftsNeeded = (targetCount + node.OutputItemCount - 1) / node.OutputItemCount;
            scalingFactor = (double)targetCraftsNeeded / cachedCraftsNeeded;
        }
        else
        {
            scalingFactor = (double)targetCount / node.Count;
        }

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
                    int scaledCount = (int)Math.Round(ingredient.Count * scalingFactor);
                    return CopyForMemo(ingredient, Math.Max(1, scaledCount)); // Ensure count is never 0
                })
                .ToList()
        };
    }
}
