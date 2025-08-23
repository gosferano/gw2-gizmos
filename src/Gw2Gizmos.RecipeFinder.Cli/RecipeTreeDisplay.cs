using System.Text;
using Gw2Gizmos.RecipeFinder.Cli.Model;

namespace Gw2Gizmos.RecipeFinder.Cli;

public static class RecipeTreeDisplay
{
    public static string GetCraftingAndBuyingPlan(RecipeNode rootNode)
    {
        var result = new StringBuilder();
        TraverseTree(rootNode, result, 0);
        return result.ToString();
    }

    public static string GetFlattenedStructure(RecipeNode rootNode)
    {
        var itemCounts = new Dictionary<int, RecipeNode>();
        AggregateItems(rootNode, itemCounts);

        var result = new StringBuilder();
        foreach ((int itemId, RecipeNode recipeNode) in itemCounts)
        {
            result.AppendLine($"- {recipeNode.Count}x {recipeNode.ItemName} for {recipeNode.BuyPrice}");
        }

        return result.ToString();
    }

    public static string GetCraftingPlan(RecipeNode rootNode)
    {
        var craftingSteps = new List<CraftingStep>();
        CollectCraftingSteps(rootNode, craftingSteps);

        // Sort by dependency order (items with no crafted ingredients first)
        var sortedSteps = SortCraftingStepsByDependency(craftingSteps);

        var result = new StringBuilder();

        foreach (var step in sortedSteps)
        {
            result.AppendLine($"- Craft {step.OutputCount}x {step.OutputName}");
            result.AppendLine($"  Requires:");
            foreach (var ingredient in step.Ingredients)
            {
                string action = ingredient.IsCrafted ? "craft" : "buy";
                result.AppendLine($"    • {ingredient.Count}x {ingredient.Name} ({action})");
            }
            result.AppendLine();
        }

        return result.ToString();
    }

    private static void CollectCraftingSteps(RecipeNode node, List<CraftingStep> craftingSteps)
    {
        // Only add crafting steps for profitable (craftable) nodes
        if (node.IsProfitable && node.Ingredients.Count > 0)
        {
            var step = new CraftingStep
            {
                ItemId = node.ItemId,
                OutputName = node.ItemName,
                OutputCount = node.Count,
                Ingredients = node
                    .Ingredients.Select(ingredient => new CraftingStepIngredient
                    {
                        ItemId = ingredient.ItemId,
                        Name = ingredient.ItemName,
                        Count = ingredient.Count,
                        IsCrafted = ingredient.IsProfitable
                    })
                    .ToList()
            };

            craftingSteps.Add(step);

            // Recursively collect crafting steps from children
            foreach (var child in node.Ingredients)
            {
                CollectCraftingSteps(child, craftingSteps);
            }
        }
    }

    private static List<CraftingStep> SortCraftingStepsByDependency(List<CraftingStep> craftingSteps)
    {
        var sorted = new List<CraftingStep>();
        var remaining = new List<CraftingStep>(craftingSteps);
        var craftedItemIds = new HashSet<int>();

        while (remaining.Count > 0)
        {
            // Find steps that don't depend on uncrafted items
            var readySteps = remaining
                .Where(step =>
                    step.Ingredients.Where(ing => ing.IsCrafted).All(ing => craftedItemIds.Contains(ing.ItemId))
                )
                .ToList();

            if (readySteps.Count == 0)
            {
                // Fallback: add remaining items in original order to avoid infinite loop
                sorted.AddRange(remaining);
                break;
            }

            // Add ready steps to sorted list
            foreach (var step in readySteps)
            {
                sorted.Add(step);
                craftedItemIds.Add(step.ItemId);
                remaining.Remove(step);
            }
        }

        return sorted;
    }

    private static void TraverseTree(RecipeNode node, StringBuilder result, int depth)
    {
        string indent = new string(' ', depth * 2);

        if (node.IsProfitable)
        {
            result.AppendLine(
                $"{indent}- Craft {node.Count}x {node.ItemName} ({node.ItemId}) (Crafting Cost: {node.CraftingCost}; Sell Price: {node.SellPrice}; Buy Price: {node.BuyPrice})"
            );
        }
        else
        {
            result.AppendLine(
                $"{indent}- Buy {node.Count}x {node.ItemName} ({node.ItemId}) (Crafting Cost: {node.CraftingCost}; Sell Price: {node.SellPrice}; Buy Price: {node.BuyPrice})"
            );
        }

        foreach (RecipeNode child in node.Ingredients)
        {
            TraverseTree(child, result, depth + 1);
        }
    }

    private static void AggregateItems(RecipeNode node, Dictionary<int, RecipeNode> itemCounts)
    {
        // If the node is not profitable, treat it as a buyable item
        if (!node.IsProfitable)
        {
            if (itemCounts.TryGetValue(node.ItemId, out var existingNode))
            {
                existingNode.Count += node.Count;
            }
            else
            {
                itemCounts[node.ItemId] = new RecipeNode
                {
                    ItemId = node.ItemId,
                    ItemName = node.ItemName,
                    Count = node.Count,
                    CraftingCostPerUnit = node.CraftingCostPerUnit,
                    SellPricePerUnit = node.SellPricePerUnit,
                    BuyPricePerUnit = node.BuyPricePerUnit
                };
            }
            return;
        }

        // If the node is profitable, process its children
        foreach (RecipeNode child in node.Ingredients)
        {
            AggregateItems(child, itemCounts);
        }
    }
}
