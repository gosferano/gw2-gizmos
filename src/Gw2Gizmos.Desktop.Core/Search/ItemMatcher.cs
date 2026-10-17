using System;
using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>An item the search can find: its id, name (and the name lower-cased once, for matching), rarity (items can
/// share a name across rarities), and whether it's tradeable (otherwise craftable-only).</summary>
public sealed record SearchableItem(int Id, string Name, string Rarity, bool Tradeable)
{
    internal string Lower { get; } = Name.ToLowerInvariant();

    internal string[] Words { get; } = Name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// Finds items by name in memory, typo-tolerant. Names starting with the query rank first, then a word starting with
/// it, then names containing it, then close spellings: the query, word for word, within an edit distance of 1 (for
/// queries of 4–7 letters) or 2 (longer) of the name's words — comparing just as much of the last word as was typed,
/// so "mystik co" finds Mystic Coin. Within a rank, tradeable items come first, then shorter names. Matching all ~74k
/// item names takes a few milliseconds.
/// </summary>
public static class ItemMatcher
{
    private const int StartsWith = 4;
    private const int WordStartsWith = 3;
    private const int Contains = 2;
    private const int CloseSpelling = 1;

    public static List<SearchableItem> Find(IReadOnlyList<SearchableItem> items, string query, int limit)
    {
        string q = query.Trim().ToLowerInvariant();
        if (q.Length == 0)
        {
            return new List<SearchableItem>();
        }

        var matches = new List<(SearchableItem Item, int Rank)>();
        foreach (SearchableItem item in items)
        {
            int rank = ExactRank(item.Lower, q);
            if (rank > 0)
            {
                matches.Add((item, rank));
            }
        }

        // Close spellings only when the exact matches don't fill the list, and only for queries long enough that a
        // typo isn't just a different word.
        int maxEdits = q.Length >= 8 ? 2 : q.Length >= 4 ? 1 : 0;
        if (matches.Count < limit && maxEdits > 0)
        {
            string[] queryWords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (SearchableItem item in items)
            {
                if (ExactRank(item.Lower, q) == 0 && IsCloseSpelling(item.Words, queryWords, maxEdits))
                {
                    matches.Add((item, CloseSpelling));
                }
            }
        }

        return matches
            .OrderByDescending(m => m.Rank)
            .ThenByDescending(m => m.Item.Tradeable)
            .ThenBy(m => m.Item.Name.Length)
            .ThenBy(m => m.Item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(m => m.Item)
            .ToList();
    }

    private static int ExactRank(string name, string query)
    {
        int index = name.IndexOf(query, StringComparison.Ordinal);
        if (index < 0)
        {
            return 0;
        }

        if (index == 0)
        {
            return StartsWith;
        }

        for (int i = index; i >= 0; i = name.IndexOf(query, i + 1, StringComparison.Ordinal))
        {
            if (!char.IsLetterOrDigit(name[i - 1]))
            {
                return WordStartsWith;
            }
        }

        return Contains;
    }

    /// <summary>
    /// Whether the query's words match consecutive words of the name within <paramref name="maxEdits"/> in total — the
    /// last query word against the start of its name word (it may be partly typed).
    /// </summary>
    private static bool IsCloseSpelling(string[] nameWords, string[] queryWords, int maxEdits)
    {
        for (int start = 0; start + queryWords.Length <= nameWords.Length; start++)
        {
            int edits = 0;
            for (int w = 0; w < queryWords.Length && edits <= maxEdits; w++)
            {
                string queryWord = queryWords[w];
                string nameWord = nameWords[start + w];
                bool last = w == queryWords.Length - 1;
                if (last && nameWord.Length > queryWord.Length)
                {
                    nameWord = nameWord[..queryWord.Length];
                }

                edits += Distance(queryWord, nameWord, maxEdits - edits);
            }

            if (edits <= maxEdits)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Levenshtein distance, giving up (returning more than <paramref name="max"/>) once it can't stay within it.</summary>
    internal static int Distance(string a, string b, int max)
    {
        if (Math.Abs(a.Length - b.Length) > max)
        {
            return max + 1;
        }

        Span<int> previous = stackalloc int[b.Length + 1];
        Span<int> current = stackalloc int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            int rowBest = current[0];
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                rowBest = Math.Min(rowBest, current[j]);
            }

            if (rowBest > max)
            {
                return max + 1;
            }

            current.CopyTo(previous);
        }

        return previous[b.Length];
    }
}
