namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// Crafting recipes the GW2 API (<c>/v2/recipes</c>) genuinely doesn't expose — the "Place of Power" combine
/// (hand-written below; verified absent from the API) and the Mystic Forge recipes scraped from the wiki
/// (<see cref="MysticForgeRecipes"/>). The engine consults these only when an item has no API recipe, so a
/// forge-only intermediate is priced from its tradeable inputs instead of dropping to 0. (Time-gated ascended
/// mats like Lump of Mithrillium are <em>in</em> the API, so they're not duplicated here.)
/// </summary>
public static class StaticRecipes
{
    public static readonly IReadOnlyList<StaticRecipe> All = Build();

    /// <summary>Recipes keyed by output item id, for the engine's "no API recipe?" fallback lookup (one per
    /// output — the hand-written craft wins, otherwise the simplest forge recipe).</summary>
    public static readonly IReadOnlyDictionary<int, StaticRecipe> ByOutputItemId =
        All.ToDictionary(recipe => recipe.OutputItemId);

    private static IReadOnlyList<StaticRecipe> Build()
    {
        // Hand-curated recipes the API truly lacks. Charged Quartz Crystal is combined at a Place of Power
        // (a map interaction, not a discipline recipe), so /v2/recipes/search?output=43772 returns nothing.
        var byOutput = new Dictionary<int, StaticRecipe>
        {
            [43772] = Recipe(43772, Ingredient(43773, 25)), // Charged Quartz Crystal — 25 Quartz Crystal
        };

        // Mystic Forge recipes from the embedded wiki scrape: keep only those fully resolved to item ids (the
        // engine prices every ingredient as an item), one per output (the simplest variant), and never override
        // a hand-curated recipe above.
        foreach (var group in MysticForgeRecipes.All
                     .Where(recipe => recipe.OutputId is not null
                         && recipe.Ingredients.Count > 0
                         && recipe.Ingredients.All(ingredient => ingredient.Id is not null))
                     .GroupBy(recipe => recipe.OutputId!.Value))
        {
            if (byOutput.ContainsKey(group.Key))
            {
                continue;
            }

            MysticForgeRecipe pick = group.OrderBy(recipe => recipe.Ingredients.Count).First();
            byOutput[group.Key] = new StaticRecipe
            {
                OutputItemId = group.Key,
                OutputItemCount = pick.OutputCount,
                Ingredients = pick.Ingredients
                    .Select(ingredient => new StaticIngredient { ItemId = ingredient.Id!.Value, Count = ingredient.Count })
                    .ToList(),
            };
        }

        return byOutput.Values.ToList();
    }

    private static StaticRecipe Recipe(int output, params StaticIngredient[] ingredients) =>
        new() { OutputItemId = output, Ingredients = ingredients };

    private static StaticIngredient Ingredient(int itemId, int count) =>
        new() { ItemId = itemId, Count = count };
}
