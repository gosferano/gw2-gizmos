namespace Gw2Gizmos.Gw2Api.Contract.V2.Recipes;

public sealed class Recipe
{
    public int Id { get; set; }
    public RecipeType Type { get; set; }
    public int OutputItemId { get; set; }
    public int OutputItemCount { get; set; }
    public int TimeToCraftMs { get; set; }
    public CraftingDisciplineName[] Disciplines { get; set; } = Array.Empty<CraftingDisciplineName>();
    public int MinRating { get; set; }
    public RecipeFlag[] Flags { get; set; } = Array.Empty<RecipeFlag>();
    public RecipeIngredient[] Ingredients { get; set; } = Array.Empty<RecipeIngredient>();
    public int? OutputUpgradeId { get; set; }
    public string ChatLink { get; set; } = null!;
}
