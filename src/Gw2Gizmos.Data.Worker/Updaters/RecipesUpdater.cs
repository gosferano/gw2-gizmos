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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecipesUpdater> _logger;
    private readonly Gw2ApiClient _apiClient;

    private const int PageSize = 200;

    public RecipesUpdater(
        IServiceScopeFactory scopeFactory,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<RecipesUpdater> logger
    )
    {
        _scopeFactory = scopeFactory;
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

                List<Recipe> mapped = apiRecipes.Select(MapToRecipeEntity).ToList();
                List<int> ids = mapped.Select(r => r.Id).ToList();

                // Fresh scope (and DbContext) per page, disposed at the end of the iteration — bounds
                // the change tracker to one page rather than accumulating every recipe for the cycle.
                using IServiceScope scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

                // One existence query for the whole page instead of one per recipe. Multiple
                // sibling collections → split query to avoid a cartesian product across the batch.
                Dictionary<int, Recipe> existingById = await dbContext
                    .Recipes.Include(r => r.Ingredients)
                    .Include(r => r.Disciplines)
                    .Include(r => r.Flags)
                    .AsSplitQuery()
                    .Where(r => ids.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, stoppingToken);

                foreach (Recipe recipe in mapped)
                {
                    if (existingById.TryGetValue(recipe.Id, out Recipe? existing))
                    {
                        dbContext.Entry(existing).CurrentValues.SetValues(recipe);

                        // SetValues copies scalars only, so the child collections are reconciled
                        // explicitly to match the API snapshot (full clear-and-re-add).
                        ReplaceCollection(existing.Ingredients, recipe.Ingredients);
                        ReplaceCollection(existing.Disciplines, recipe.Disciplines);
                        ReplaceCollection(existing.Flags, recipe.Flags);
                    }
                    else
                    {
                        await dbContext.Recipes.AddAsync(recipe, stoppingToken);
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);

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

    // Replaces a tracked child collection with the incoming snapshot: clearing orphans the old
    // rows (cascade-deleted via their required FK) and the incoming children are inserted.
    private static void ReplaceCollection<TChild>(ICollection<TChild> existing, ICollection<TChild> incoming)
    {
        existing.Clear();
        foreach (TChild child in incoming)
        {
            existing.Add(child);
        }
    }
}
