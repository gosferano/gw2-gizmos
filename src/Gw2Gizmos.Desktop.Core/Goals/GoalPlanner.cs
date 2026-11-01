using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>An item and how many, for one line of what a goal needs: covered, to buy or that can't be bought.
/// </summary>
/// <param name="Copper">What buying them costs at the lowest listings (or a vendor's coin), all of them.</param>
/// <param name="Note">
/// How a part that can't be bought is had (e.g. "world completion"), or what a vendor charges.
/// </param>
public sealed record GoalLine(int ItemId, string Name, long Count, long Copper = 0, string? Note = null);

/// <summary>
/// A currency goals need (karma, spirit shards, or an item a vendor takes as payment), and how much of it the account
/// has towards them, after the goals before took theirs. Shown with <see cref="IconUrl"/>, or else the icon of
/// <see cref="ItemId"/>.
/// </summary>
public sealed record GoalCurrency(string Name, long Needed, long Have, int? CurrencyId, int? ItemId, string? IconUrl)
{
    public long Missing => Math.Max(0, Needed - Have);
}

/// <summary>
/// What a goal still needs, as <see cref="GoalPlanner"/> worked it out: what holdings cover, what to buy and for how
/// much, what to buy from vendors for other currencies, the currencies that takes, and the parts that can't be bought.
/// <see cref="Value"/> is what the whole goal costs; <see cref="CoveredValue"/> what holdings cover of that.
/// </summary>
public sealed record GoalNeeds(
    Goal Goal,
    IReadOnlyList<GoalLine> Covered,
    IReadOnlyList<GoalLine> ToBuy,
    IReadOnlyList<GoalLine> FromVendors,
    IReadOnlyList<GoalCurrency> Currencies,
    IReadOnlyList<GoalLine> CantBuy,
    decimal Value,
    decimal CoveredValue)
{
    public long BuyCopper => ToBuy.Sum(line => line.Copper) + FromVendors.Sum(line => line.Copper);

    /// <summary>The share of the goal's cost holdings cover, 0 to 1.</summary>
    public double Progress => Value > 0
        ? (double)Math.Min(1, CoveredValue / Value)
        : ToBuy.Count + FromVendors.Count + CantBuy.Count == 0 ? 1 : 0;

    /// <summary>Whether holdings cover all of it.</summary>
    public bool Done => ToBuy.Count + FromVendors.Count + CantBuy.Count == 0
        && Currencies.All(currency => currency.Missing == 0);
}

/// <summary>What every goal needs, and how many of each held item they set aside between them.</summary>
public sealed record GoalPlan(IReadOnlyList<GoalNeeds> Goals, IReadOnlyDictionary<int, long> Reserved)
{
    public static GoalPlan Empty { get; } = new([], new Dictionary<int, long>());

    public long BuyCopper => Goals.Sum(goal => goal.BuyCopper);
}

/// <summary>
/// Works out what goals still need from the account's holdings. Each goal follows its recipe tree (the cost engine's
/// cheapest recipes) from the top: at each part, as many as are held are set aside first, so a held precursor covers
/// its whole branch, and only the rest is made or bought. The rest is made where crafting it costs less than buying
/// it, or where it can't be bought; otherwise it's bought (on the trading post, or from a vendor where that's
/// cheaper), and what it's made from isn't needed, even when held: selling that and buying the part costs less. Goals
/// draw on the same holdings and wallet in order, so the first goal gets a material before the second.
/// </summary>
public static class GoalPlanner
{
    public static GoalPlan Plan(
        IReadOnlyList<(Goal Goal, RecipeNode Tree)> goals,
        IReadOnlyDictionary<int, long> held,
        IReadOnlyDictionary<int, long> wallet)
    {
        var items = new Dictionary<int, long>(held);
        var currencies = new Dictionary<int, long>(wallet);
        var reserved = new Dictionary<int, long>();
        List<GoalNeeds> needs = goals
            .Select(goal => new Walk(items, currencies, reserved).Run(goal.Goal, goal.Tree))
            .ToList();
        return new GoalPlan(needs, reserved);
    }

    /// <summary>
    /// Whether to make the rest of a part rather than buy it: when it can't be bought, or what can be bought of its
    /// recipe costs less than buying it. Parts of the recipe that can't be bought (a precursor's account-bound
    /// collection items) count as nothing, so the part is still made and they show as needed, as a goal's own
    /// precursor usually is.
    /// </summary>
    private static bool Craft(RecipeNode node)
    {
        decimal? coin = node.BuyPrice > 0 ? node.BuyPrice : null;
        decimal? acquire = (coin, node.VendorCoinCost) switch
        {
            ({ } c, { } v) => Math.Min(c, v),
            (null, var v) => v,
            (var c, null) => c,
        };
        if (acquire is null)
        {
            return true;
        }

        decimal craft = node.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost);
        return craft < acquire;
    }

    private static decimal Unit(RecipeNode node) => node.Count > 0 ? node.EffectiveCost / node.Count : 0;

    /// <summary>One goal's walk down its tree, taking from the holdings and wallet left by the goals before.</summary>
    private sealed class Walk(Dictionary<int, long> items, Dictionary<int, long> wallet, Dictionary<int, long> reserved)
    {
        private readonly Lines _covered = new();
        private readonly Lines _toBuy = new();
        private readonly Lines _fromVendors = new();
        private readonly Lines _cantBuy = new();
        private readonly Dictionary<string, GoalCurrency> _currencies = new();
        private decimal _coveredValue;

        public GoalNeeds Run(Goal goal, RecipeNode tree)
        {
            Need(tree, goal.Count);
            return new GoalNeeds(goal, _covered.ToList(), _toBuy.ToList(), _fromVendors.ToList(),
                _currencies.Values.OrderByDescending(c => c.Missing).ThenBy(c => c.Name).ToList(), _cantBuy.ToList(),
                Unit(tree) * goal.Count, _coveredValue);
        }

        private void Need(RecipeNode node, long count)
        {
            if (count <= 0)
            {
                return;
            }

            if (node.IsCurrency)
            {
                Spend(node.ItemName, count, currencyId: node.ItemId, itemId: null, iconUrl: node.CurrencyIconUrl);
                return;
            }

            long take = Take(node.ItemId, count);
            if (take > 0)
            {
                _covered.Add(node, take);
                _coveredValue += take * Unit(node);
            }

            long rest = count - take;
            if (rest == 0)
            {
                return;
            }

            if (node.IsCraftable && Craft(node))
            {
                // The tree's counts are for the crafts its own count took; scale them to the crafts the rest takes.
                decimal treeCrafts = Math.Max(1, Math.Ceiling(node.Count / node.OutputItemCount));
                decimal crafts = Math.Ceiling(rest / node.OutputItemCount);
                foreach (RecipeNode child in node.Ingredients)
                {
                    Need(child, (long)Math.Ceiling(child.Count / treeCrafts * crafts));
                }

                return;
            }

            Acquire(node, rest);
        }

        // Buys the rest: on the trading post, or from a vendor where that costs less (or it's only sold there); else
        // it can't be bought.
        private void Acquire(RecipeNode node, long count)
        {
            decimal? coin = node.BuyPricePerUnit > 0 ? (decimal)node.BuyPricePerUnit * count : null;
            decimal? vendor = node.VendorCoinCostPerUnit * count;
            if (coin is { } copper && (vendor is null || copper <= vendor))
            {
                _toBuy.Add(node, count, (long)copper);
            }
            else if (node.PrimaryVendorOffer is { } offer)
            {
                long bundles = (count + offer.Quantity - 1) / offer.Quantity;
                long vendorCopper = 0;
                foreach (VendorCost cost in offer.Cost)
                {
                    long amount = cost.Amount * bundles;
                    if (cost.IsCoin)
                    {
                        vendorCopper += amount;
                    }
                    else
                    {
                        Spend(cost.Currency, amount, cost.CurrencyId, cost.ItemId, cost.IconUrl);
                    }
                }

                string price = string.Join(" + ", offer.Cost.Select(cost =>
                    cost.IsCoin ? Coin.Format(cost.Amount * bundles) : $"{cost.Amount * bundles:N0} {cost.Currency}"));
                _fromVendors.Add(node, count, vendorCopper, price);
            }
            else
            {
                _cantBuy.Add(node, count, note: node.AcquiredVia ?? (node.IsVendorAcquirable ? "a vendor" : null));
            }
        }

        // Sets aside up to `count` of a held item.
        private long Take(int itemId, long count)
        {
            long take = Math.Min(count, items.GetValueOrDefault(itemId));
            if (take > 0)
            {
                items[itemId] -= take;
                reserved[itemId] = reserved.GetValueOrDefault(itemId) + take;
            }

            return take;
        }

        // Spends a currency from the wallet, or an item a vendor takes as payment from the holdings. A wallet
        // currency that's also an item (Spirit Shards, Research Notes have legacy items) is spent from the wallet,
        // shown with the item's icon when it has no currency icon.
        private void Spend(string name, long amount, int? currencyId, int? itemId, string? iconUrl)
        {
            long have = 0;
            if (currencyId is { } id)
            {
                have = Math.Min(amount, wallet.GetValueOrDefault(id));
                wallet[id] = wallet.GetValueOrDefault(id) - have;
                itemId = iconUrl is null ? itemId : null;
            }
            else if (itemId is { } item)
            {
                have = Take(item, amount);
            }

            string key = currencyId is { } c ? $"currency:{c}" : itemId is { } i ? $"item:{i}" : $"name:{name}";
            GoalCurrency sum = _currencies.GetValueOrDefault(key) ?? new GoalCurrency(name, 0, 0, currencyId, itemId,
                iconUrl);
            _currencies[key] = sum with { Needed = sum.Needed + amount, Have = sum.Have + have };
        }
    }

    /// <summary>Lines summed by item, most costly first, then by name.</summary>
    private sealed class Lines
    {
        private readonly Dictionary<int, GoalLine> _lines = new();

        public void Add(RecipeNode node, long count, long copper = 0, string? note = null)
        {
            GoalLine line = _lines.GetValueOrDefault(node.ItemId) ?? new GoalLine(node.ItemId, node.ItemName, 0);
            // Vendor prices are worded for the whole purchase, so a second purchase of the same item adds its own.
            string? notes = line.Note is null ? note : note is null ? line.Note : $"{line.Note}; {note}";
            _lines[node.ItemId] = line with { Count = line.Count + count, Copper = line.Copper + copper, Note = notes };
        }

        public List<GoalLine> ToList() =>
            _lines.Values.OrderByDescending(line => line.Copper).ThenBy(line => line.Name).ToList();
    }
}

/// <summary>What goals can draw on: held items by id, and wallet currencies by id.</summary>
public sealed record GoalHoldings(IReadOnlyDictionary<int, long> Items, IReadOnlyDictionary<int, long> Wallet)
{
    public static GoalHoldings Empty { get; } = new(new Dictionary<int, long>(), new Dictionary<int, long>());
}
