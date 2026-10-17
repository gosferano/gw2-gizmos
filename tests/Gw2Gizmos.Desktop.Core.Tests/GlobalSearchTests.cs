namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>The global search's name matching: start of the name, then start of a word, then anywhere.</summary>
public sealed class GlobalSearchTests
{
    [Theory]
    [InlineData("Mystic Coin", "mys", 3)]
    [InlineData("Mystic Coin", "coin", 2)]
    [InlineData("Lion's Arch", "arch", 2)]
    [InlineData("Tequatl the Sunless", "sun", 2)]
    [InlineData("Firebrand", "brand", 1)]
    [InlineData("Mystic Coin", "ic co", 1)]
    [InlineData("Mystic Coin", "clover", 0)]
    [InlineData(null, "coin", 0)]
    public void Score_RanksStartThenWordThenAnywhere(string? text, string query, int expected) =>
        Assert.Equal(expected, GlobalSearch.Score(text, query));

    [Fact]
    public void Score_FindsALaterWordStartEvenAfterAMidWordMatch()
    {
        // The first "coin" is mid-word (in "Bitcoin"); the second starts a word.
        Assert.Equal(2, GlobalSearch.Score("Bitcoin Coin", "coin"));
    }
}
