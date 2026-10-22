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
/// A craft worth more than the materials it takes. Per craft (the first, made from what's held now): the result's
/// instant-sell value, the owned materials it uses and what they'd sell for, what the missing ones cost to buy, and the
/// ingredients it crafts first from stock (e.g. ingots from owned ore). Then how many can be made from stock alone,
/// each craft taking what the ones before it left (<see cref="Runs"/>). <see cref="Known"/> says whether the account
/// knows the recipe (null when its learned recipes aren't synced).
/// </summary>
public sealed record StockCraft(
    StockRecipe Recipe,
    long OutputCopper,
    IReadOnlyList<StockIngredient> Uses,
    IReadOnlyList<StockIngredient> Missing,
    IReadOnlyList<StockIngredient> Crafted,
    IReadOnlyList<(int Count, long Profit)> Runs,
    bool CharactersCanCraft,
    bool? Known)
{
    /// <summary>
    /// How many of the result instant sales took in the last week (buy orders filled), or null without the price
    /// history to tell.
    /// </summary>
    public int? SoldPerWeek { get; init; }

    /// <summary>How many can be made from stock alone, each still profitable.</summary>
    public int CanMake => Runs.Sum(r => r.Count);

    /// <summary>How many to make: as many as stock allows, but no more than a week of sales takes.</summary>
    public int ToMake => SoldPerWeek is { } sold ? Math.Min(CanMake, sold / Recipe.OutputCount) : CanMake;

    public long UsesCopper => Uses.Sum(u => u.Copper);

    public long MissingCopper => Missing.Sum(m => m.Copper);

    /// <summary>What one craft makes over selling the owned materials (and buying the missing ones).</summary>
    public long ProfitCopper => OutputCopper - UsesCopper - MissingCopper;

    /// <summary>What crafting <see cref="ToMake"/> from stock makes; later ones can make less, once they have to
    /// craft what the first took from stock.</summary>
    public long StockProfitCopper
    {
        get
        {
            long total = 0;
            int left = ToMake;
            foreach ((int count, long profit) in Runs)
            {
                int made = Math.Min(left, count);
                total += made * profit;
                left -= made;
            }

            return total;
        }
    }
}

/// <summary>
/// What's worth crafting from materials already owned. Using an owned material costs what it would sell for
/// instantly (its value as the account's worth counts it), not what it would cost to buy, and one that can't be sold
/// (e.g. a time-gated Lump of Mithrillium) costs what crafting another would; a craft is worth it when its result
/// sells instantly for more. An ingredient that isn't held can be crafted from what is (ingots from owned ore), a few
/// steps deep, when that costs less than buying it; it has to trade on the trading post, which keeps time-gated
/// materials out, and its recipe has to be one the characters can craft and the account knows. A craft can be short
/// of some materials, bought at their lowest listing, as long as the owned ones are worth at least as much as those.
/// The result has to sell on the trading post: an account-bound result (e.g. an ascended material) has no price to
/// make a profit with, so it's left out.
/// </summary>
public static class CraftFromStock
{
    // The trading post keeps 15% of a sale: the 5% listing and 10% exchange fees, as the account's value counts them.
    private const long TakePercent = 85;

    // How many steps down an ingredient can be crafted from stock (e.g. ore → ingot → plated dowel).
    private const int MaxDepth = 3;

    // Enough to tell "plenty" without simulating a stack of ore one craft at a time.
    private const int MaxCrafts = 10_000;

    /// <param name="recipes">Every recipe made only of items.</param>
    /// <param name="held">The account's holdings: each item's total across its storage, bank and bags.</param>
    /// <param name="heldValues">What one of each held item sells for instantly (the account value's rule).</param>
    /// <param name="craftCosts">What crafting one of an item costs, for the held ones that don't sell.</param>
    /// <param name="prices">The latest highest buy order and lowest listing per item, 0 when there's none.</param>
    /// <param name="ratings">The highest level the account's characters have in each discipline.</param>
    /// <param name="learned">The recipes the account has learned, or null when they aren't synced.</param>
    public static List<StockCraft> Find(
        IReadOnlyList<StockRecipe> recipes,
        IReadOnlyDictionary<int, int> held,
        IReadOnlyDictionary<int, long> heldValues,
        IReadOnlyDictionary<int, long> craftCosts,
        IReadOnlyDictionary<int, (int Buy, int Sell)> prices,
        IReadOnlyDictionary<string, int> ratings,
        IReadOnlySet<int>? learned)
    {
        var planner = new Planner(recipes, held, heldValues, craftCosts, prices, ratings, learned);
        var crafts = new List<StockCraft>();
        foreach (StockRecipe recipe in recipes)
        {
            if (planner.Craft(recipe) is { ProfitCopper: > 0 } craft)
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

    /// <summary>What one craft takes from stock, buys and crafts on the way, each by item.</summary>
    private sealed class Plan
    {
        public Dictionary<int, int> Taken { get; private init; } = [];

        public Dictionary<int, long> TakenCopper { get; private init; } = [];

        public HashSet<int> AtCraftCost { get; private init; } = [];

        public Dictionary<int, int> Bought { get; private init; } = [];

        public Dictionary<int, long> BoughtCopper { get; private init; } = [];

        public Dictionary<int, int> Crafted { get; private init; } = [];

        public bool Failed { get; set; }

        public long Cost => TakenCopper.Values.Sum() + BoughtCopper.Values.Sum();

        public int TakenUnits => Taken.Values.Sum();

        public void Take(int itemId, int count, long copper, bool atCraftCost)
        {
            Taken[itemId] = Taken.GetValueOrDefault(itemId) + count;
            TakenCopper[itemId] = TakenCopper.GetValueOrDefault(itemId) + copper;
            if (atCraftCost)
            {
                AtCraftCost.Add(itemId);
            }
        }

        public void Buy(int itemId, int count, long copper)
        {
            Bought[itemId] = Bought.GetValueOrDefault(itemId) + count;
            BoughtCopper[itemId] = BoughtCopper.GetValueOrDefault(itemId) + copper;
        }

        public Plan Clone() => new()
        {
            Taken = new(Taken), TakenCopper = new(TakenCopper), AtCraftCost = [.. AtCraftCost],
            Bought = new(Bought), BoughtCopper = new(BoughtCopper), Crafted = new(Crafted), Failed = Failed,
        };
    }

    private sealed class Planner
    {
        private readonly IReadOnlyDictionary<int, int> _held;
        private readonly IReadOnlyDictionary<int, long> _heldValues;
        private readonly IReadOnlyDictionary<int, long> _craftCosts;
        private readonly IReadOnlyDictionary<int, (int Buy, int Sell)> _prices;
        private readonly IReadOnlyDictionary<string, int> _ratings;
        private readonly IReadOnlySet<int>? _learned;

        // The recipes an ingredient can be crafted from stock with, by the item they make.
        private readonly Dictionary<int, List<StockRecipe>> _subRecipes;

        // The items stock reaches: held, or craftable from held ones within MaxDepth steps.
        private readonly HashSet<int> _reachable;

        public Planner(
            IReadOnlyList<StockRecipe> recipes,
            IReadOnlyDictionary<int, int> held,
            IReadOnlyDictionary<int, long> heldValues,
            IReadOnlyDictionary<int, long> craftCosts,
            IReadOnlyDictionary<int, (int Buy, int Sell)> prices,
            IReadOnlyDictionary<string, int> ratings,
            IReadOnlySet<int>? learned)
        {
            _held = held;
            _heldValues = heldValues;
            _craftCosts = craftCosts;
            _prices = prices;
            _ratings = ratings;
            _learned = learned;
            _subRecipes = recipes
                .Where(r => prices.GetValueOrDefault(r.OutputItemId).Sell > 0 && CanUseForIngredient(r))
                .GroupBy(r => r.OutputItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _reachable = held.Where(h => h.Value > 0).Select(h => h.Key).ToHashSet();
            for (int depth = 0; depth < MaxDepth; depth++)
            {
                List<int> more = _subRecipes
                    .Where(pair => !_reachable.Contains(pair.Key)
                        && pair.Value.Any(r => r.Ingredients.Any(i => _reachable.Contains(i.ItemId))))
                    .Select(pair => pair.Key)
                    .ToList();
                _reachable.UnionWith(more);
            }
        }

        public StockCraft? Craft(StockRecipe recipe)
        {
            int outputBuy = _prices.GetValueOrDefault(recipe.OutputItemId).Buy;
            if (outputBuy <= 0 || recipe.Ingredients.Count == 0
                || !recipe.Ingredients.Any(i => _reachable.Contains(i.ItemId)))
            {
                return null;
            }

            long output = (long)outputBuy * TakePercent / 100 * recipe.OutputCount;
            var spent = new Dictionary<int, int>();
            Plan? first = Plan(recipe, spent);
            // Mostly from stock: the owned materials carry at least as much of the craft as the bought ones.
            if (first is null || first.Taken.Count == 0
                || first.BoughtCopper.Values.Sum() > first.TakenCopper.Values.Sum()
                || output - first.Cost <= 0)
            {
                return null;
            }

            bool charactersCanCraft = CharactersCanCraft(recipe);
            // A recipe learned at a discipline's level is known once a character has the level, listed or not.
            bool? known = _learned is null
                ? null
                : _learned.Contains(recipe.Id) || (recipe.AutoLearned && charactersCanCraft);
            return new StockCraft(
                recipe,
                output,
                first.Taken.Select(t => new StockIngredient(
                    t.Key, t.Value, first.TakenCopper[t.Key], first.AtCraftCost.Contains(t.Key))).ToList(),
                first.Bought.Select(b => new StockIngredient(b.Key, b.Value, first.BoughtCopper[b.Key])).ToList(),
                first.Crafted.Select(c => new StockIngredient(c.Key, c.Value, 0)).ToList(),
                Runs(recipe, output, first, spent),
                charactersCanCraft,
                known);
        }

        // Crafts made from stock alone, one after another, each from what the ones before left, as long as each is
        // still profitable. A plan that stock covers several times over is repeated in one go.
        private List<(int Count, long Profit)> Runs(
            StockRecipe recipe, long output, Plan first, Dictionary<int, int> spent)
        {
            var runs = new List<(int Count, long Profit)>();
            int made = 0;
            Plan? plan = first;
            while (plan is { Bought.Count: 0 } && output - plan.Cost > 0 && made < MaxCrafts)
            {
                int times = Math.Min(MaxCrafts - made, plan.Taken.Min(t => Available(t.Key, spent) / t.Value));
                runs.Add((times, output - plan.Cost));
                made += times;
                foreach ((int itemId, int count) in plan.Taken)
                {
                    spent[itemId] = spent.GetValueOrDefault(itemId) + count * times;
                }

                plan = Plan(recipe, spent);
            }

            return runs;
        }

        private Plan? Plan(StockRecipe recipe, IReadOnlyDictionary<int, int> spent)
        {
            var plan = new Plan();
            foreach ((int itemId, int count) in recipe.Ingredients)
            {
                Need(plan, itemId, count, 1, spent);
                if (plan.Failed)
                {
                    return null;
                }
            }

            return plan;
        }

        // Covers `count` of an item: from stock first, then whichever costs least of buying the rest or crafting it
        // from stock. Fails when the rest can't be bought or crafted.
        private void Need(Plan plan, int itemId, int count, int depth, IReadOnlyDictionary<int, int> spent)
        {
            int take = Math.Min(Math.Max(0, Available(itemId, spent) - plan.Taken.GetValueOrDefault(itemId)), count);
            if (take > 0)
            {
                long value = _heldValues.GetValueOrDefault(itemId);
                bool atCraftCost = value == 0 && _craftCosts.TryGetValue(itemId, out value);
                plan.Take(itemId, take, take * value, atCraftCost);
            }

            int rest = count - take;
            if (rest == 0)
            {
                return;
            }

            Plan? best = null;
            int sell = _prices.GetValueOrDefault(itemId).Sell;
            if (sell > 0)
            {
                best = plan.Clone();
                best.Buy(itemId, rest, (long)rest * sell);
            }

            if (depth <= MaxDepth && _reachable.Contains(itemId)
                && _subRecipes.TryGetValue(itemId, out List<StockRecipe>? subRecipes))
            {
                foreach (StockRecipe sub in subRecipes)
                {
                    int crafts = (rest + sub.OutputCount - 1) / sub.OutputCount;
                    Plan trial = plan.Clone();
                    trial.Crafted[itemId] = trial.Crafted.GetValueOrDefault(itemId) + crafts * sub.OutputCount;
                    foreach ((int subItemId, int subCount) in sub.Ingredients)
                    {
                        Need(trial, subItemId, subCount * crafts, depth + 1, spent);
                        if (trial.Failed)
                        {
                            break;
                        }
                    }

                    // Only worth it when it puts stock to use, and costs less than the alternatives.
                    if (!trial.Failed && trial.TakenUnits > plan.TakenUnits && (best is null || trial.Cost < best.Cost))
                    {
                        best = trial;
                    }
                }
            }

            if (best is null)
            {
                plan.Failed = true;
                return;
            }

            Copy(best, plan);
        }

        private int Available(int itemId, IReadOnlyDictionary<int, int> spent) =>
            _held.GetValueOrDefault(itemId) - spent.GetValueOrDefault(itemId);

        private static void Copy(Plan from, Plan to)
        {
            Replace(to.Taken, from.Taken);
            Replace(to.TakenCopper, from.TakenCopper);
            to.AtCraftCost.Clear();
            to.AtCraftCost.UnionWith(from.AtCraftCost);
            Replace(to.Bought, from.Bought);
            Replace(to.BoughtCopper, from.BoughtCopper);
            Replace(to.Crafted, from.Crafted);
            to.Failed = from.Failed;
        }

        private static void Replace<T>(Dictionary<int, T> to, Dictionary<int, T> from)
        {
            to.Clear();
            foreach ((int key, T value) in from)
            {
                to[key] = value;
            }
        }

        private bool CharactersCanCraft(StockRecipe recipe) =>
            recipe.Disciplines.Any(d => _ratings.GetValueOrDefault(d, -1) >= recipe.MinRating);

        // An ingredient's recipe has to be one the account can use: known, at a level a character has (each unknown
        // when not synced).
        private bool CanUseForIngredient(StockRecipe recipe)
        {
            bool canCraft = _ratings.Count == 0 || CharactersCanCraft(recipe);
            bool known = _learned is null || _learned.Contains(recipe.Id) || recipe.AutoLearned;
            return canCraft && known;
        }
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
