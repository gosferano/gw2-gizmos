namespace Gw2Gizmos.Gw2Api.Contract.Recipes;

public class RecipeIngredient
{
    public int Id { get; set; }
    public int Count { get; set; }
    public RecipeIngredientType Type { get; set; }
}