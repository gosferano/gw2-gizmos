using System;
using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A recipe as craft from stock reads it: its output and its item ingredients, per craft.</summary>
public sealed record StockRecipe(
    int Id,
    int OutputItemId,
    int OutputCount,
    int MinRating,
    IReadOnlyList<string> Disciplines,
    bool LearnedFromItem,
    bool AutoLearned,
    IReadOnlyList<(int ItemId, int Count)> Ingredients);

/// <summary>
/// An ingredient of one craft: how many it takes, and what they're worth or cost (copper, all of them). An owned one
/// that can't be sold is worth what it costs to craft another (<see cref="AtCraftCost"/>).
/// </summary>
public sealed record StockIngredient(int ItemId, int Count, long Copper, bool AtCraftCost = false);

/// <summary>
/// A craft worth more than the materials it takes: per craft, the result's instant-sell value, what the owned
/// materials it uses would sell for, and what the missing ones cost to buy; and how many can be made from stock alone.
/// <see cref="Known"/> says whether the account knows the recipe (null when its learned recipes aren't synced).
/// </summary>
public sealed record StockCraft(
    StockRecipe Recipe,
    long OutputCopper,
    IReadOnlyList<StockIngredient> Uses,
    IReadOnlyList<StockIngredient> Missing,
    int CanMake,
    bool CharactersCanCraft,
    bool? Known)
{
    /// <summary>
    /// How many of the result instant sales took in the last week (buy orders filled), or null without the price
    /// history to tell.
    /// </summary>
    public int? SoldPerWeek { get; init; }

    /// <summary>How many to make: as many as stock allows, but no more than a week of sales takes.</summary>
    public int ToMake => SoldPerWeek is { } sold ? Math.Min(CanMake, sold / Recipe.OutputCount) : CanMake;

    public long UsesCopper => Uses.Sum(u => u.Copper);

    public long MissingCopper => Missing.Sum(m => m.Copper);

    /// <summary>What one craft makes over selling the owned materials (and buying the missing ones).</summary>
    public long ProfitCopper => OutputCopper - UsesCopper - MissingCopper;

    /// <summary>What crafting <see cref="ToMake"/> from stock makes.</summary>
    public long StockProfitCopper => ProfitCopper * ToMake;
}

/// <summary>
/// What's worth crafting from materials already owned. Using an owned material costs what it would sell for
/// instantly (its value as the account's worth counts it), not what it would cost to buy, and one that can't be sold
/// (e.g. a time-gated Lump of Mithrillium) costs what crafting another would; a craft is worth it when
/// its result sells instantly for more. A craft can be short of some materials, bought at their lowest listing, as
/// long as the owned ones are worth at least as much as those. The result has to sell on the trading post: an
/// account-bound result (e.g. an ascended material) has no price to make a profit with, so it's left out.
/// </summary>
public static class CraftFromStock
{
    // The trading post keeps 15% of a sale: the 5% listing and 10% exchange fees, as the account's value counts them.
    private const long TakePercent = 85;

    /// <param name="recipes">Every recipe made only of items.</param>
    /// <param name="held">The account's holdings: each item's total across its storage, bank and bags.</param>
    /// <param name="heldValues">What one of each held item sells for instantly (the account value's rule).</param>
    /// <param name="craftCosts">What crafting one of an item costs, for the held ones that don't sell.</param>
    /// <param name="prices">The latest highest buy order and lowest listing per item, 0 when there's none.</param>
    /// <param name="ratings">The highest level the account's characters have in each discipline.</param>
    /// <param name="learned">The recipes the account has learned, or null when they aren't synced.</param>
    public static List<StockCraft> Find(
        IEnumerable<StockRecipe> recipes,
        IReadOnlyDictionary<int, int> held,
        IReadOnlyDictionary<int, long> heldValues,
        IReadOnlyDictionary<int, long> craftCosts,
        IReadOnlyDictionary<int, (int Buy, int Sell)> prices,
        IReadOnlyDictionary<string, int> ratings,
        IReadOnlySet<int>? learned)
    {
        var crafts = new List<StockCraft>();
        foreach (StockRecipe recipe in recipes)
        {
            if (Craft(recipe, held, heldValues, craftCosts, prices, ratings, learned) is { ProfitCopper: > 0 } craft)
            {
                crafts.Add(craft);
            }
        }

        return crafts;
    }

    /// <summary>
    /// Each craft with how many of its result sold instantly in the last week (<paramref name="soldPerWeek"/>, by
    /// item), which caps how many are worth making; a result nobody sold to a buy order all week is left out, as its
    /// buy order is no price it would sell at.
    /// </summary>
    public static List<StockCraft> LimitToMarket(
        IEnumerable<StockCraft> crafts, IReadOnlyDictionary<int, int> soldPerWeek) =>
        crafts
            .Select(c => c with { SoldPerWeek = soldPerWeek.GetValueOrDefault(c.Recipe.OutputItemId) })
            .Where(c => c.SoldPerWeek >= c.Recipe.OutputCount)
            .ToList();

    private static StockCraft? Craft(
        StockRecipe recipe,
        IReadOnlyDictionary<int, int> held,
        IReadOnlyDictionary<int, long> heldValues,
        IReadOnlyDictionary<int, long> craftCosts,
        IReadOnlyDictionary<int, (int Buy, int Sell)> prices,
        IReadOnlyDictionary<string, int> ratings,
        IReadOnlySet<int>? learned)
    {
        int outputBuy = prices.GetValueOrDefault(recipe.OutputItemId).Buy;
        if (outputBuy <= 0 || recipe.Ingredients.Count == 0)
        {
            return null;
        }

        var uses = new List<StockIngredient>();
        var missing = new List<StockIngredient>();
        int canMake = int.MaxValue;
        foreach ((int itemId, int count) in recipe.Ingredients)
        {
            int owned = held.GetValueOrDefault(itemId);
            canMake = Math.Min(canMake, owned / count);
            int used = Math.Min(owned, count);
            if (used > 0)
            {
                long value = heldValues.GetValueOrDefault(itemId);
                uses.Add(value == 0 && craftCosts.TryGetValue(itemId, out long craftCost)
                    ? new StockIngredient(itemId, used, used * craftCost, AtCraftCost: true)
                    : new StockIngredient(itemId, used, used * value));
            }

            if (count > used)
            {
                int sell = prices.GetValueOrDefault(itemId).Sell;
                if (sell <= 0)
                {
                    return null; // Short of something that can't be bought.
                }

                missing.Add(new StockIngredient(itemId, count - used, (long)(count - used) * sell));
            }
        }

        // Mostly from stock: the owned materials carry at least as much of the craft as the bought ones.
        if (uses.Count == 0 || missing.Sum(m => m.Copper) > uses.Sum(u => u.Copper))
        {
            return null;
        }

        bool charactersCanCraft = recipe.Disciplines.Any(d => ratings.GetValueOrDefault(d, -1) >= recipe.MinRating);
        long output = (long)outputBuy * TakePercent / 100 * recipe.OutputCount;
        // A recipe learned at a discipline's level is known once a character has the level, listed or not.
        bool? known = learned is null
            ? null
            : learned.Contains(recipe.Id) || (recipe.AutoLearned && charactersCanCraft);
        return new StockCraft(recipe, output, uses, missing, canMake, charactersCanCraft, known);
    }
}

/// <summary>
/// The crafts worth making from stock, the names of the items they make and take, and whether any character's
/// crafting levels and the account's learned recipes are known (without them, nothing can be said about who can
/// craft what, or which recipes the account knows).
/// </summary>
public sealed record StockCrafts(
    IReadOnlyList<StockCraft> Crafts, IReadOnlyDictionary<int, string> Names, bool HasRatings, bool HasLearned)
{
    public static StockCrafts Empty { get; } = new([], new Dictionary<int, string>(), false, false);
}
