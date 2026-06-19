using System.IO.Compression;
using System.Text.Json;

namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// All Mystic Forge recipes, scraped from the wiki (see <c>tools/Gw2Gizmos.Wiki.DataScraper</c>) and embedded
/// whole as gzipped JSON (<c>Crafting/Data/mystic-forge-recipes.json.gz</c>). This is the full, display-ready
/// data (names + source page); <see cref="StaticRecipes"/> derives the id-only view the pricing engine needs.
/// </summary>
public static class MysticForgeRecipes
{
    // Declared before All: static fields initialize in textual order, and All = Load() reads KnownChances.
    /// <summary>
    /// Success chance for the few recipes whose output isn't guaranteed. The wiki documents these only in prose
    /// (not a parseable template field), so they're maintained here by hand; every other recipe defaults to 1.0.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, double> KnownChances = new Dictionary<int, double>
    {
        [19675] = 0.31, // Mystic Clover — ~31% chance per forge (community drop research)
    };

    public static readonly IReadOnlyList<MysticForgeRecipe> All = Load();

    /// <summary>Recipes grouped by output item id (an item can have several forge recipes), for the UI to show
    /// how a craftable item is forged. Recipes whose output has no game id are omitted.</summary>
    public static readonly IReadOnlyDictionary<int, IReadOnlyList<MysticForgeRecipe>> ByOutputItemId = All
        .Where(recipe => recipe.OutputId is not null)
        .GroupBy(recipe => recipe.OutputId!.Value)
        .ToDictionary(group => group.Key, group => (IReadOnlyList<MysticForgeRecipe>)group.ToList());

    private static IReadOnlyList<MysticForgeRecipe> Load()
    {
        // Options inlined, not a static field — static fields init in textual order, and `All` above runs Load()
        // before a later field would be set (a null options arg silently breaks the case-insensitive match).
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        using Stream compressed = EmbeddedData.Open("mystic-forge-recipes.json.gz");
        using var json = new GZipStream(compressed, CompressionMode.Decompress);
        RecipeCatalog? catalog = JsonSerializer.Deserialize<RecipeCatalog>(json, options);
        IReadOnlyList<MysticForgeRecipe> recipes = catalog?.Recipes ?? [];

        // Fill in known success chances (absent from the scrape — see KnownChances); the rest keep their 1.0.
        return recipes
            .Select(recipe => recipe.OutputId is { } id && KnownChances.TryGetValue(id, out double chance)
                ? recipe with { Chance = chance }
                : recipe)
            .ToList();
    }

    private sealed record RecipeCatalog
    {
        public IReadOnlyList<MysticForgeRecipe> Recipes { get; init; } = [];
    }
}
