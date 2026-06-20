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
    /// <summary>Recipes keyed by output item id for the engine's "no API recipe?" fallback. An output can have
    /// several forge variants (e.g. the 1- and 10-forge Mystic Clover recipes); all usable ones are kept so the
    /// engine can price each and take the cheapest with real prices, rather than guessing a variant here. A
    /// hand-written craft, where present, is the sole recipe for its output.</summary>
    public static readonly IReadOnlyDictionary<int, IReadOnlyList<StaticRecipe>> ByOutputItemId = Build();

    /// <summary>Every curated recipe, flattened across outputs.</summary>
    public static readonly IReadOnlyList<StaticRecipe> All = ByOutputItemId.Values.SelectMany(recipes => recipes).ToList();

    private static IReadOnlyDictionary<int, IReadOnlyList<StaticRecipe>> Build()
    {
        // Hand-curated recipes the API truly lacks. Charged Quartz Crystal is combined at a Place of Power
        // (a map interaction, not a discipline recipe), so /v2/recipes/search?output=43772 returns nothing.
        var byOutput = new Dictionary<int, IReadOnlyList<StaticRecipe>>
        {
            [43772] = [Recipe(43772, Ingredient(43773, 25))], // Charged Quartz Crystal — 25 Quartz Crystal
        };

        // Mystic Forge recipes from the embedded wiki scrape: keep every usable variant per output (see IsUsable),
        // never overriding a hand-curated recipe above. The engine compares variants by real cost and picks the
        // cheapest, so no variant is preferred here.
        foreach (var group in MysticForgeRecipes.All
                     .Where(IsUsable)
                     .GroupBy(recipe => recipe.OutputId!.Value))
        {
            if (byOutput.ContainsKey(group.Key))
            {
                continue;
            }

            byOutput[group.Key] = group.Select(ToStaticRecipe).ToList();
        }

        return byOutput;
    }

    private static StaticRecipe ToStaticRecipe(MysticForgeRecipe recipe) => new()
    {
        OutputItemId = recipe.OutputId!.Value,
        // Expected (average) yield, kept exact (not rounded) — material promotions produce a random range and
        // chance-based recipes only sometimes succeed, so the per-output cost is the ingredient cost divided by
        // this average count produced, not by 1.
        OutputItemCount = (decimal)recipe.ExpectedOutputCount,
        Ingredients = recipe.Ingredients
            .Select(ingredient => new StaticIngredient { ItemId = ingredient.Id!.Value, Count = ingredient.Count })
            .ToList(),
    };

    /// <summary>
    /// Whether a scraped forge recipe can be used to price its output. Requires every name to have resolved to a
    /// game item id (the engine prices each ingredient as an item). A self-referential recipe — the output is
    /// also an ingredient, i.e. a material promotion that seeds the forge with some of the target tier — is only
    /// usable when it yields strictly more than that catalyst; a net-zero recipe (e.g. 1 Rurik's Engagement Ring
    /// → 1 Rurik's Engagement Ring) is circular and prices to nonsense, so it's dropped.
    /// </summary>
    private static bool IsUsable(MysticForgeRecipe recipe)
    {
        if (recipe.OutputId is not { } outputId
            || recipe.Ingredients.Count == 0
            || recipe.Ingredients.Any(ingredient => ingredient.Id is null))
        {
            return false;
        }

        // A minipet is never a sensible crafting input for a non-mini output — the wiki lists oddball forge
        // recipes like "Obsidian Shard from a Mini Risen Priest of Balthazar" that price commodity materials by
        // sacrificing collectibles. Mini-combine recipes that *produce* a mini are legitimate and kept.
        if (!IsMini(recipe.Output) && recipe.Ingredients.Any(ingredient => IsMini(ingredient.Name)))
        {
            return false;
        }

        int catalyst = recipe.Ingredients
            .Where(ingredient => ingredient.Id == outputId)
            .Sum(ingredient => ingredient.Count);
        return catalyst == 0 || recipe.ExpectedOutputCount > catalyst;
    }

    private static bool IsMini(string name) =>
        name.StartsWith("Mini ", StringComparison.Ordinal) || name.StartsWith("Miniature ", StringComparison.Ordinal);

    private static StaticRecipe Recipe(int output, params StaticIngredient[] ingredients) =>
        new() { OutputItemId = output, Ingredients = ingredients };

    private static StaticIngredient Ingredient(int itemId, int count) =>
        new() { ItemId = itemId, Count = count };
}
