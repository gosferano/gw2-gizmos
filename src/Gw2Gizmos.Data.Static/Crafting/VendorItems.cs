using System.IO.Compression;
using System.Text.Json;

namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// The full NPC vendor catalog — the GW2 API has no vendor <em>buy</em>-price endpoint, so it's scraped from
/// the wiki (see <c>tools/Gw2Gizmos.Wiki.DataScraper</c>) and embedded whole as a gzipped JSON
/// (<c>Crafting/Data/vendor-items.json.gz</c>, ~750 KB compressed from ~22 MB). Every vendor/currency/price is
/// kept for display; the recipe engine uses <see cref="CopperPriceFor"/> as a price floor for items the
/// trading post doesn't list (or where a vendor undercuts it), so they aren't valued at 0.
/// </summary>
public static class VendorItems
{
    public static readonly IReadOnlyList<VendorItem> All = Load();

    /// <summary>Vendor items keyed by item id, for the engine's price-fallback lookup.</summary>
    public static readonly IReadOnlyDictionary<int, VendorItem> ByItemId = All
        .Where(item => item.GameId is not null)
        .GroupBy(item => item.GameId!.Value)
        .ToDictionary(group => group.Key, group => group.First());

    /// <summary>The cheapest per-unit copper price for an item if any vendor sells it for coin; otherwise null.</summary>
    public static int? CopperPriceFor(int itemId) =>
        ByItemId.TryGetValue(itemId, out VendorItem? item) ? item.CopperPerUnit : null;

    private static IReadOnlyList<VendorItem> Load()
    {
        // Options are inlined (not a static field): static fields initialize in textual order, and `All` above
        // runs Load() before a field declared later would be set — a null options arg silently breaks the
        // case-insensitive "items" match.
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        using Stream compressed = EmbeddedData.Open("vendor-items.json.gz");
        using var json = new GZipStream(compressed, CompressionMode.Decompress);
        VendorCatalog? catalog = JsonSerializer.Deserialize<VendorCatalog>(json, options);
        return catalog?.Items ?? [];
    }

    private sealed record VendorCatalog
    {
        public IReadOnlyList<VendorItem> Items { get; init; } = [];
    }
}
