using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for crafting recipes from <c>/v2/recipes</c>, including their
/// ingredients, disciplines, and flags.
/// </summary>
public class RecipesUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<RecipesUpdater> _logger;
    private readonly Gw2ApiClient _apiClient;

    private const int PageSize = 200;

    public RecipesUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<RecipesUpdater> logger
    )
    {
        _dbContext = dbContext;
        _apiClient = apiClientFactory.Create(Locale.English);
        _logger = logger;
    }

    public async Task UpdateRecipes(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting recipes update...");

        // Fetch all recipe IDs from the API
        int[]? recipeIds = await _apiClient.V2.Recipes.GetIds(stoppingToken);

        if (recipeIds is null || recipeIds.Length == 0)
        {
            _logger.LogWarning("Recipes API returned no ids; skipping recipes update.");
            return;
        }

        _logger.LogInformation("Total recipes with data: {Count}", recipeIds.Length);

        for (var i = 0; i < recipeIds.Length; i += PageSize)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                int[] pageIds = recipeIds.Skip(i).Take(PageSize).ToArray();

                _logger.LogInformation(
                    "Processing recipes {Start} to {End} of {Total}",
                    i + 1,
                    i + pageIds.Length,
                    recipeIds.Length
                );

                Gw2Api.Contract.V2.Recipes.Recipe[]? apiRecipes = await _apiClient.V2.Recipes.GetByIds(
                    pageIds,
                    stoppingToken
                );

                if (apiRecipes is null || apiRecipes.Length == 0)
                {
                    _logger.LogWarning(
                        "Recipes API returned no data for {Count} id(s); they may have been removed.",
                        pageIds.Length
                    );
                    continue;
                }

                foreach (Gw2Api.Contract.V2.Recipes.Recipe apiRecipe in apiRecipes)
                {
                    Recipe recipe = MapToRecipeEntity(apiRecipe);
                    await AddOrUpdateRecipe(recipe, stoppingToken);
                }

                await _dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Processed items {Start} to {End}. Total processed: {Total}",
                    i + 1,
                    i + pageIds.Length,
                    i + pageIds.Length
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recipes {Start} to {End}", i + 1, i + PageSize);
            }
        }

        _logger.LogInformation("Recipes update completed.");
    }

    private static Recipe MapToRecipeEntity(Gw2Api.Contract.V2.Recipes.Recipe apiRecipe)
    {
        return new Recipe
        {
            Id = apiRecipe.Id,
            Type = apiRecipe.Type,
            OutputItemId = apiRecipe.OutputItemId,
            OutputItemCount = apiRecipe.OutputItemCount,
            TimeToCraftMs = apiRecipe.TimeToCraftMs,
            Disciplines = apiRecipe
                .Disciplines.Select(d => new RecipeDiscipline { Value = d.Value, RecipeId = apiRecipe.Id })
                .ToList(),
            MinRating = apiRecipe.MinRating,
            Flags = apiRecipe.Flags.Select(f => new RecipeFlag { Value = f.Value, RecipeId = apiRecipe.Id }).ToList(),
            Ingredients = apiRecipe
                .Ingredients.Select(i => new RecipeIngredient
                {
                    Id = i.Id,
                    Count = i.Count,
                    Type = i.Type,
                    RecipeId = apiRecipe.Id
                })
                .ToList(),
            OutputUpgradeId = apiRecipe.OutputUpgradeId,
            ChatLink = apiRecipe.ChatLink
        };
    }

    private async Task AddOrUpdateRecipe(Recipe recipe, CancellationToken stoppingToken)
    {
        Recipe? existingRecipe = await _dbContext
            .Recipes.Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id, stoppingToken);

        if (existingRecipe != null)
        {
            // Update existing recipe
            _dbContext.Entry(existingRecipe).CurrentValues.SetValues(recipe);
            existingRecipe.Ingredients.Clear();
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                existingRecipe.Ingredients.Add(ingredient);
            }
        }
        else
        {
            // Add new recipe
            await _dbContext.Recipes.AddAsync(recipe, stoppingToken);
        }
    }
}
