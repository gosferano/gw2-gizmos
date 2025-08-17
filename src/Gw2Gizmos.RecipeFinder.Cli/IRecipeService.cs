using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

namespace Gw2Gizmos.RecipeFinder.Cli;

public interface IRecipeService
{
    Task<Recipe?> GetRecipeAsync(int itemId, CancellationToken ct);
    Task<Recipe[]> GetAllRecipesAsync(CancellationToken ct);
}
