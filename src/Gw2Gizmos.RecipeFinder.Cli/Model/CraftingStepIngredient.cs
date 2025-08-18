namespace Gw2Gizmos.RecipeFinder.Cli.Model;

public class CraftingStepIngredient
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsCrafted { get; set; }
}
