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
        recipes = recipes
            .Select(recipe => recipe.OutputId is { } id && KnownChances.TryGetValue(id, out double chance)
                ? recipe with { Chance = chance }
                : recipe)
            .ToList();
        return ResolveNames(recipes);
    }

    /// <summary>
    /// Fills in the ids of ingredients the scrape couldn't resolve because the wiki names them in the plural
    /// ("Obsidian Shards", "Globs of Ectoplasm") or with a section link ("…#item1"): matched, singular or plural, to
    /// an item the forge recipes or the vendor catalog do have an id for. A recipe with such an ingredient would
    /// otherwise be dropped (e.g. Gift of Expertise's, leaving only vendor offers that take ectoplasm). A name that
    /// matches no single item ("Exotic staff", "Charm (ingredient)") stays unresolved.
    /// </summary>
    private static IReadOnlyList<MysticForgeRecipe> ResolveNames(IReadOnlyList<MysticForgeRecipe> recipes)
    {
        var ids = new Dictionary<string, HashSet<int>>();
        void Know(string name, int? id)
        {
            if (id is { } known)
            {
                string key = Key(name);
                if (!ids.TryGetValue(key, out HashSet<int>? set))
                {
                    ids[key] = set = [];
                }

                set.Add(known);
            }
        }

        foreach (MysticForgeRecipe recipe in recipes)
        {
            Know(recipe.Output, recipe.OutputId);
            foreach (MysticForgeIngredient ingredient in recipe.Ingredients)
            {
                Know(ingredient.Name, ingredient.Id);
            }
        }

        foreach (VendorItem item in VendorItems.All)
        {
            Know(item.Item, item.GameId);
            foreach (CostComponent cost in item.Offers.SelectMany(offer => offer.Cost))
            {
                Know(cost.Currency, cost.ItemId);
            }
        }

        return recipes
            .Select(recipe => recipe.Ingredients.Any(i => i.Id is null)
                ? recipe with
                {
                    Ingredients = recipe.Ingredients
                        .Select(i => i.Id is null && ids.TryGetValue(Key(i.Name), out HashSet<int>? set)
                            && set.Count == 1
                                ? i with { Id = set.Single() }
                                : i)
                        .ToList(),
                }
                : recipe)
            .ToList();
    }

    // A name as matched: lower-cased, without a section link, each word's plural "s" trimmed ("Piles of Bloodstone
    // Dust" → "pile of bloodstone dust").
    private static string Key(string name) => string.Join(' ', name.Split('#')[0].Trim().ToLowerInvariant()
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => word.Length > 3 && word.EndsWith('s') ? word[..^1] : word));

    private sealed record RecipeCatalog
    {
        public IReadOnlyList<MysticForgeRecipe> Recipes { get; init; } = [];
    }
}
