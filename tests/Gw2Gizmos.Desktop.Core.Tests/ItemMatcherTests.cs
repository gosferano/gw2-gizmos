namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Finding items by name in memory: ranked by how the name matches, tolerant of typos.</summary>
public sealed class ItemMatcherTests
{
    private static readonly SearchableItem[] Items =
    {
        new(1, "Mystic Coin", "Rare", true),
        new(2, "Bag of Mystic Coins", "Basic", false),
        new(3, "Coin of Remembrance", "Rare", false),
        new(4, "Glob of Ectoplasm", "Exotic", true),
        new(5, "Fiber Splicer", "Exotic", true),
        new(6, "Fiber Splicer", "Ascended", false),
        new(7, "Superior Rune of the Scholar", "Exotic", true),
    };

    private static string[] Find(string query, int limit = 8) =>
        ItemMatcher.Find(Items, query, limit).Select(i => $"{i.Name} ({i.Rarity})").ToArray();

    [Fact]
    public void Ranks_starts_with_then_word_start_then_contains()
    {
        Assert.Equal(
            new[] { "Coin of Remembrance (Rare)", "Mystic Coin (Rare)", "Bag of Mystic Coins (Basic)" },
            Find("coin"));
    }

    [Theory]
    [InlineData("mystik coin")]          // a typo
    [InlineData("mystik co")]            // a typo, the last word half typed
    [InlineData("MYSTIC COIN")]          // case
    public void Finds_the_item_despite_typos_or_case(string query) =>
        Assert.Equal("Mystic Coin (Rare)", Find(query)[0]);

    [Fact]
    public void Tolerates_more_typos_in_longer_queries()
    {
        Assert.Equal("Glob of Ectoplasm (Exotic)", Find("glbo of ectoplasm")[0]);
        Assert.Equal("Superior Rune of the Scholar (Exotic)", Find("superor rune of the scholr")[0]);
    }

    [Fact]
    public void Short_queries_arent_fuzzy() => Assert.Empty(Find("cpn"));

    [Fact]
    public void Same_named_items_of_different_rarities_are_both_found_tradeable_first() =>
        Assert.Equal(new[] { "Fiber Splicer (Exotic)", "Fiber Splicer (Ascended)" }, Find("fiber splicer"));

    [Theory]
    [InlineData("kitten", "sitting", 3, 3)]
    [InlineData("coin", "coin", 1, 0)]
    [InlineData("mystik", "mystic", 1, 1)]
    [InlineData("abc", "xyzxyz", 1, 2)] // over the limit: reported as limit + 1
    public void Distance_is_capped_at_the_limit(string a, string b, int max, int expected) =>
        Assert.Equal(expected, ItemMatcher.Distance(a, b, max));
}
