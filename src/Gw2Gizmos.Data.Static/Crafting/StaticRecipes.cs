namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// Hardcoded crafting recipes that aren't in the GW2 API (<c>/v2/recipes</c>) — Mystic Forge combines and the
/// daily "Place of Power" / cooldown crafts. The recipe engine consults these when an item has no API recipe,
/// so an account-bound intermediate (e.g. Charged Quartz Crystal) is priced from its tradeable inputs rather
/// than dropping to 0.
/// </summary>
public static class StaticRecipes
{
    public static readonly IReadOnlyList<StaticRecipe> All = new[]
    {
        Recipe(output: 43772, Ingredient(43773, 25)), // Charged Quartz Crystal
        Recipe(output: 46742, Ingredient(19684, 50), Ingredient(19721, 1), Ingredient(46747, 10)), // Lump of Mithrillium
        Recipe(output: 46744, Ingredient(19709, 50), Ingredient(19721, 1), Ingredient(46747, 10)), // Glob of Elder Spirit Residue
        Recipe(output: 46745, Ingredient(19735, 50), Ingredient(19721, 1), Ingredient(46747, 10)), // Spool of Thick Elonian Cord
        Recipe(output: 46740, Ingredient(19747, 100), Ingredient(19721, 1), Ingredient(19790, 25)), // Spool of Silk Weaving Thread
        // TODO: Mystic Forge recipes (precursors, gift components, etc.).
    };

    /// <summary>Recipes keyed by output item id, for the engine's "no API recipe?" fallback lookup.</summary>
    public static readonly IReadOnlyDictionary<int, StaticRecipe> ByOutputItemId =
        All.ToDictionary(recipe => recipe.OutputItemId);

    private static StaticRecipe Recipe(int output, params StaticIngredient[] ingredients) =>
        new() { OutputItemId = output, Ingredients = ingredients };

    private static StaticRecipe Recipe(int output, int outputCount, params StaticIngredient[] ingredients) =>
        new() { OutputItemId = output, OutputItemCount = outputCount, Ingredients = ingredients };

    private static StaticIngredient Ingredient(int itemId, int count) =>
        new() { ItemId = itemId, Count = count };
}
