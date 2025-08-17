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
        var stack = new Stack<(RecipeNode Node, int ItemId, int Multiplier, bool Processed)>();
        stack.Push((rootNode, rootItemId, parentMultiplier, false));

        while (stack.Count > 0)
        {
            var (currentNode, itemId, multiplier, processed) = stack.Pop();

            if (processed)
            {
                // Calculate crafting cost after processing all children
                if (currentNode.Ingredients.Count > 0)
                {
                    currentNode.CraftingCost = currentNode.Ingredients.Sum(child =>
                        child.IsCraftable && child.CraftingCost < child.BuyPrice ? child.CraftingCost : child.BuyPrice
                    );
                }

                // Calculate profitability
                currentNode.IsProfitable =
                    currentNode.CraftingCost > 0
                    && (currentNode.CraftingCost < currentNode.BuyPrice || currentNode.SellPrice == 0);
                continue;
            }

            // Push the current node back as processed
            stack.Push((currentNode, itemId, multiplier, true));

            // Fetch prices
            TradingPostPrices tradingPostPrices = await _priceService.GetPricesAsync(itemId, ct);
            currentNode.BuyPrice = tradingPostPrices.SellOrderPrice * currentNode.Count;
            currentNode.SellPrice = tradingPostPrices.BuyOrderPrice * currentNode.Count;

            // Fetch item name with fallback
            var itemName = await _itemService.GetItemNameAsync(itemId, ct);
            currentNode.ItemName = !string.IsNullOrWhiteSpace(itemName) ? itemName : $"Unknown Item {itemId}";

            // Fetch recipe
            var recipe = await _recipeService.GetRecipeAsync(itemId, ct);

            if (recipe != null)
            {
                foreach (var ingredient in recipe.Ingredients)
                {
                    int childMultiplier = ingredient.Count * multiplier;
                    var childNode = new RecipeNode { ItemId = ingredient.Id, Count = childMultiplier };
                    currentNode.Ingredients.Add(childNode);
                    stack.Push((childNode, ingredient.Id, childMultiplier, false));
                }
            }
        }

        return rootNode;
    }
}
