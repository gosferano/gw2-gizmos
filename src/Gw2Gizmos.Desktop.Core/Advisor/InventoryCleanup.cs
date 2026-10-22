using System;
using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A stack in one of the account's slots: the bank, shared inventory or a character's bags.</summary>
/// <param name="Place">"Bank", "Shared inventory" or the character's name.</param>
/// <param name="HasCharges">
/// Whether it's a tool with charges (salvage kits, gathering tools), which don't stack.
/// </param>
public sealed record HeldStack(string Place, int ItemId, int Count, bool HasCharges = false);

/// <summary>What cleanup needs to know about an item: its type, and whether it binds to a character.</summary>
public sealed record CleanupItem(string Type, bool Soulbound);

/// <summary>The kinds of cleanup suggestion, in the order the page shows them.</summary>
public enum CleanupKind
{
    /// <summary>Partial stacks of the same item that fit in fewer slots.</summary>
    Stack,

    /// <summary>Materials in bags that material storage has room for.</summary>
    Deposit,

    /// <summary>Materials beyond what material storage holds, sitting in bags.</summary>
    Overflow,

    /// <summary>Containers waiting to be opened.</summary>
    Open,

    /// <summary>Items nothing in the account uses, worth selling.</summary>
    Sell,

    /// <summary>Items only finished achievements asked for.</summary>
    Leftover,
}

/// <summary>
/// One suggestion: what to do with how many of an item, where they are, and what they're worth (copper, all of them;
/// 0 when it doesn't matter).
/// </summary>
public sealed record CleanupSuggestion(
    CleanupKind Kind, int ItemId, int Count, long Copper, IReadOnlyList<(string Place, int Count)> Places)
{
    /// <summary>For a stack: how many slots the item takes now, and how few it would.</summary>
    public (int Now, int After)? Slots { get; init; }

    /// <summary>For a leftover: the finished achievements that asked for it.</summary>
    public IReadOnlyList<string> DoneFor { get; init; } = [];
}

/// <summary>Everything cleanup reads, gathered from the account's synced data.</summary>
public sealed record CleanupInput(
    IReadOnlyList<HeldStack> Stacks,
    IReadOnlyDictionary<int, int> Storage,
    IReadOnlySet<int> StorageTakes,
    int StorageCapacity,
    IReadOnlyDictionary<int, CleanupItem> Items,
    IReadOnlyDictionary<int, long> Values,
    IReadOnlySet<int> Used,
    IReadOnlyDictionary<int, IReadOnlyList<string>> DoneFor);

/// <summary>
/// What to clean up in the account's bank, shared inventory and bags, each suggestion with its reason: partial stacks
/// that fit in fewer slots; materials that material storage has room for, and those beyond it; containers to open;
/// items worth selling that nothing in the account uses (no recipe takes them, no unfinished achievement asks for
/// them); and leftovers, which only finished achievements asked for (worth something or not). Material storage itself
/// is only read for how full it is.
/// </summary>
public static class InventoryCleanup
{
    /// <summary>How many of an item a slot holds.</summary>
    public const int StackSize = 250;

    // Types whose items stack even when every one held is a single.
    private static readonly HashSet<string> StackingTypes =
        ["CraftingMaterial", "Trophy", "Consumable", "Container", "UpgradeComponent"];

    /// <summary>
    /// How much material storage likely holds per item, as the API doesn't say: the fullest slot, rounded up to a
    /// storage upgrade (250 each).
    /// </summary>
    public static int GuessCapacity(IReadOnlyDictionary<int, int> storage)
    {
        int fullest = storage.Values.DefaultIfEmpty(0).Max();
        return Math.Max(StackSize, (fullest + StackSize - 1) / StackSize * StackSize);
    }

    public static List<CleanupSuggestion> Suggest(CleanupInput input)
    {
        var suggestions = new List<CleanupSuggestion>();
        foreach (IGrouping<int, HeldStack> item in input.Stacks.Where(s => s.Count > 0).GroupBy(s => s.ItemId))
        {
            int itemId = item.Key;
            int total = item.Sum(s => s.Count);
            CleanupItem? info = input.Items.GetValueOrDefault(itemId);
            long value = input.Values.GetValueOrDefault(itemId);
            List<(string Place, int Count)> places = item
                .GroupBy(s => s.Place)
                .Select(g => (g.Key, g.Sum(s => s.Count)))
                .ToList();

            if (input.StorageTakes.Contains(itemId))
            {
                // A material goes to storage, as far as there's room; what's beyond it is only taking up slots.
                int room = Math.Max(0, input.StorageCapacity - input.Storage.GetValueOrDefault(itemId));
                int deposit = Math.Min(room, total);
                if (deposit > 0)
                {
                    suggestions.Add(new CleanupSuggestion(CleanupKind.Deposit, itemId, deposit, 0, places));
                }

                if (total > deposit)
                {
                    suggestions.Add(new CleanupSuggestion(
                        CleanupKind.Overflow, itemId, total - deposit, (total - deposit) * value, places));
                }

                continue;
            }

            if (Stack(item.ToList(), info, places) is { } stack)
            {
                suggestions.Add(stack);
            }

            if (info?.Type == "Container")
            {
                suggestions.Add(new CleanupSuggestion(CleanupKind.Open, itemId, total, 0, places));
            }
            else if (!input.Used.Contains(itemId)
                     && input.DoneFor.TryGetValue(itemId, out IReadOnlyList<string>? doneFor))
            {
                suggestions.Add(new CleanupSuggestion(CleanupKind.Leftover, itemId, total, total * value, places)
                {
                    DoneFor = doneFor,
                });
            }
            else if (value > 0 && !input.Used.Contains(itemId))
            {
                suggestions.Add(new CleanupSuggestion(CleanupKind.Sell, itemId, total, total * value, places));
            }
        }

        return suggestions;
    }

    // Partial stacks that fit in fewer slots. A tool with charges doesn't stack, nor does equipment; an item that
    // binds to a character only stacks with copies on the same character, so it's left alone when it's on several.
    private static CleanupSuggestion? Stack(
        List<HeldStack> stacks, CleanupItem? info, List<(string Place, int Count)> places)
    {
        List<HeldStack> stackable = stacks.Where(s => !s.HasCharges).ToList();
        bool canStack = stackable.Any(s => s.Count > 1) || (info is not null && StackingTypes.Contains(info.Type));
        if (stackable.Count < 2 || !canStack || (info?.Soulbound == true && places.Count > 1))
        {
            return null;
        }

        int count = stackable.Sum(s => s.Count);
        int after = (count + StackSize - 1) / StackSize;
        return after < stackable.Count
            ? new CleanupSuggestion(CleanupKind.Stack, stacks[0].ItemId, count, 0, places)
            {
                Slots = (stackable.Count, after),
            }
            : null;
    }
}

/// <summary>
/// The cleanup suggestions, the names of their items, and the storage capacity guessed from what's stored.
/// </summary>
public sealed record CleanupData(
    IReadOnlyList<CleanupSuggestion> Suggestions, IReadOnlyDictionary<int, string> Names, int GuessedCapacity)
{
    public static CleanupData Empty { get; } = new([], new Dictionary<int, string>(), InventoryCleanup.StackSize);
}
