namespace Gw2Gizmos.Gw2Api.Contract.V2.Recipes;

public sealed class RecipeIngredient
{
    public int Id { get; set; }
    public int Count { get; set; }
    public RecipeIngredientType Type { get; set; }
}
