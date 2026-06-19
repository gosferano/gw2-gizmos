namespace Gw2Gizmos.RecipeFinder.Model;

public class CraftingStep
{
    public int ItemId { get; set; }
    public string OutputName { get; set; } = string.Empty;
    public long OutputCount { get; set; }
    public List<CraftingStepIngredient> Ingredients { get; set; } = new();
}
