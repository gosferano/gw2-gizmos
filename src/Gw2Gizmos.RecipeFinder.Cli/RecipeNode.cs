namespace Gw2Gizmos.RecipeFinder.Cli;

public class RecipeNode
{
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CraftingCost { get; set; }
    public bool IsProfitable { get; set; }
    public int Count { get; set; }
    public List<RecipeNode> Ingredients { get; set; } = new();
}
