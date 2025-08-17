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
        var itemCounts = new Dictionary<int, (string ItemName, int Count)>();
        AggregateItems(rootNode, itemCounts);

        var result = new StringBuilder();
        foreach (var (itemId, (itemName, count)) in itemCounts)
        {
            result.AppendLine($"- {count}x {itemName} ({itemId})");
        }

        return result.ToString();
    }

    private static void TraverseTree(RecipeNode node, StringBuilder result, int depth)
    {
        string indent = new string(' ', depth * 2);

        if (node.IsProfitable)
        {
            result.AppendLine(
                $"{indent}- Craft {node.Count}x {node.ItemName} ({node.ItemId}) (Crafting Cost: {node.CraftingCost}, Sell Price: {node.SellPrice})"
            );
        }
        else
        {
            result.AppendLine(
                $"{indent}- Buy {node.Count}x {node.ItemName} ({node.ItemId}) (Crafting Cost: {node.CraftingCost}, Sell Price: {node.SellPrice})"
            );
        }

        foreach (var child in node.Ingredients)
        {
            TraverseTree(child, result, depth + 1);
        }
    }

    private static void AggregateItems(RecipeNode node, Dictionary<int, (string ItemName, int Count)> itemCounts)
    {
        // If the node is not profitable, treat it as a buyable item
        if (!node.IsProfitable)
        {
            if (itemCounts.TryGetValue(node.ItemId, out (string ItemName, int Count) value))
            {
                itemCounts[node.ItemId] = (node.ItemName, value.Count + node.Count);
            }
            else
            {
                itemCounts[node.ItemId] = (node.ItemName, node.Count);
            }
            return;
        }

        // If the node is profitable, process its children
        foreach (var child in node.Ingredients)
        {
            AggregateItems(child, itemCounts);
        }
    }
}
