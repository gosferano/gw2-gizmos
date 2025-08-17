namespace Gw2Gizmos.RecipeFinder.Cli;

public class RecipeNode
{
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public int SellPricePerUnit { get; set; }
    public int BuyPricePerUnit { get; set; }
    public decimal CraftingCostPerUnit { get; set; }
    public int Count { get; set; }
    public int OutputItemCount { get; set; } = 1;
    public List<RecipeNode> Ingredients { get; set; } = new();
    public bool IsCraftable => Ingredients.Count > 0;

    public int SellPrice => SellPricePerUnit * Count;
    public int BuyPrice => BuyPricePerUnit * Count;
    public decimal CraftingCost => CraftingCostPerUnit * Count;
    public bool IsProfitable =>
        CraftingCostPerUnit > 0 && (CraftingCostPerUnit < BuyPricePerUnit || SellPricePerUnit == 0);
}
