using System.Text;

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

    private static void TraverseTree(RecipeNode node, StringBuilder result, int depth)
    {
        string indent = new string(' ', depth * 2);

        if (node.IsProfitable)
        {
            result.AppendLine(
                $"{indent}- Craft {node.Count}x {node.ItemName} ({node.ItemId}) (Crafting Cost: {node.CraftingCost}, Sell Price: {node.SellPrice}, Buy Price: {node.BuyPrice})"
            );
        }
        else
        {
            result.AppendLine(
                $"{indent}- Buy {node.Count}x {node.ItemName} ({node.ItemId}) (Crafting Cost: {node.CraftingCost}, Sell Price: {node.SellPrice}, Buy Price: {node.BuyPrice})"
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
