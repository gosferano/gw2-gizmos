using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.RecipeFinder.Cli;

public class RecipeService : IRecipeService
{
    private readonly Gw2GizmosDbContext _dbContext;

    public RecipeService(Gw2GizmosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Recipe?> GetRecipeAsync(int itemId, CancellationToken ct)
    {
        return await _dbContext
            .Recipes.Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.OutputItemId == itemId, ct);
    }

    public async Task<Recipe[]> GetAllRecipesAsync(CancellationToken ct)
    {
        return await _dbContext.Recipes.Include(r => r.Ingredients).ToArrayAsync(ct);
    }
}
