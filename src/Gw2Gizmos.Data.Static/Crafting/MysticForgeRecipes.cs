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
        return catalog?.Recipes ?? [];
    }

    private sealed record RecipeCatalog
    {
        public IReadOnlyList<MysticForgeRecipe> Recipes { get; init; } = [];
    }
}
