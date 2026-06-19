namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// Crafting recipes the GW2 API (<c>/v2/recipes</c>) doesn't expose — daily "Place of Power" / cooldown crafts
/// (hand-written below) and the Mystic Forge recipes scraped from the wiki (<see cref="MysticForgeRecipes"/>).
/// The recipe engine consults these when an item has no API recipe, so an account-bound intermediate (e.g. a
/// forge-only material) is priced from its tradeable inputs instead of dropping to 0.
/// </summary>
public static class StaticRecipes
{
    public static readonly IReadOnlyList<StaticRecipe> All = Build();

    /// <summary>Recipes keyed by output item id, for the engine's "no API recipe?" fallback lookup (one per
    /// output — hand-written crafts win, otherwise the simplest forge recipe).</summary>
    public static readonly IReadOnlyDictionary<int, StaticRecipe> ByOutputItemId =
        All.ToDictionary(recipe => recipe.OutputItemId);

    private static IReadOnlyList<StaticRecipe> Build()
    {
        // Hand-curated recipes (daily cooldown crafts, not in the API and not Mystic Forge).
        var byOutput = new Dictionary<int, StaticRecipe>
        {
            [43772] = Recipe(43772, Ingredient(43773, 25)), // Charged Quartz Crystal
            [46742] = Recipe(46742, Ingredient(19684, 50), Ingredient(19721, 1), Ingredient(46747, 10)), // Lump of Mithrillium
            [46744] = Recipe(46744, Ingredient(19709, 50), Ingredient(19721, 1), Ingredient(46747, 10)), // Glob of Elder Spirit Residue
            [46745] = Recipe(46745, Ingredient(19735, 50), Ingredient(19721, 1), Ingredient(46747, 10)), // Spool of Thick Elonian Cord
            [46740] = Recipe(46740, Ingredient(19747, 100), Ingredient(19721, 1), Ingredient(19790, 25)), // Spool of Silk Weaving Thread
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
