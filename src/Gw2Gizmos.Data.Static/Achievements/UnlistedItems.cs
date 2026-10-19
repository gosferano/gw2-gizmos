using System.IO.Compression;
using System.Text.Json;
using Gw2Gizmos.Data.Static.Crafting;

namespace Gw2Gizmos.Data.Static.Achievements;

/// <summary>
/// Names of the items achievement objectives refer to that the API's <c>/v2/items</c> doesn't list (e.g. a legendary
/// collection's journals and relics), from the wiki (see <c>tools/Gw2Gizmos.Wiki.DataScraper</c>, <c>unlisted-items</c>
/// mode), embedded as gzipped JSON (<c>Achievements/Data/unlisted-items.json.gz</c>). Without them those objectives
/// could only show their item id.
/// </summary>
public static class UnlistedItems
{
    /// <summary>The in-game name of each, keyed by item id.</summary>
    public static readonly IReadOnlyDictionary<int, string> NamesById = Load();

    private static IReadOnlyDictionary<int, string> Load()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        using Stream compressed = EmbeddedData.Open("unlisted-items.json.gz");
        using var json = new GZipStream(compressed, CompressionMode.Decompress);
        Catalog? catalog = JsonSerializer.Deserialize<Catalog>(json, options);
        return (catalog?.Items ?? []).ToDictionary(item => item.Id, item => item.Name);
    }

    private sealed record Catalog
    {
        public IReadOnlyList<Entry> Items { get; init; } = [];
    }

    private sealed record Entry(int Id, string Name);
}
