namespace Gw2Gizmos.RecipeFinder.Model;

public class RecipeNode
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public int SellPricePerUnit { get; set; }
    public int BuyPricePerUnit { get; set; }
    public decimal CraftingCostPerUnit { get; set; }

    // long, not int: material-promotion chains (e.g. promoting Copper Ore all the way up) multiply the required
    // count ~250x per tier, which blows past int.MaxValue and would wrap negative. The cycle guard keeps the
    // tree finite, but the quantities along it are still genuinely in the billions.
    public long Count { get; set; }
    public int OutputItemCount { get; set; } = 1;

    /// <summary>Depth from the root (root = 0), set after the tree is built. Drives default expansion so a huge
    /// tree (a legendary's promotion chains) doesn't render every node at once and freeze the UI.</summary>
    public int Depth { get; set; }

    /// <summary>Whether this node is expanded by default in the craft tree — only the top couple of levels, so
    /// the rest renders lazily when the user expands it.</summary>
    public bool AutoExpand => Depth < 2;
    public bool IsCurrency { get; set; }
    public List<RecipeNode> Ingredients { get; set; } = new();
    public bool IsCraftable => Ingredients.Count > 0;

    // long: deep trees (e.g. the Agony Infusion doubling chain) push Count into the millions, so a copper
    // total of pricePerUnit * Count easily exceeds int.MaxValue (~214,748g) and would wrap negative.
    public long SellPrice => (long)SellPricePerUnit * Count;
    public long BuyPrice => (long)BuyPricePerUnit * Count;
    public decimal CraftingCost => CraftingCostPerUnit * Count;
    public bool IsProfitable =>
        CraftingCostPerUnit > 0 && (CraftingCostPerUnit < BuyPricePerUnit || SellPricePerUnit == 0);

    /// <summary>Crafting is strictly cheaper (or the only available way) to obtain this node — bolded in the
    /// recipe tree. When craft and buy are equal, neither is bolded; a 0 on either side means that option
    /// isn't available rather than "free".</summary>
    public bool CraftIsCheaper => CraftingCost > 0 && (BuyPrice <= 0 || CraftingCost < BuyPrice);

    /// <summary>Buying is strictly cheaper (or the only available way) to obtain this node — bolded in the
    /// recipe tree. Equal craft/buy bolds neither.</summary>
    public bool BuyIsCheaper => BuyPrice > 0 && (CraftingCost <= 0 || BuyPrice < CraftingCost);

    /// <summary>
    /// The cheapest known cost to obtain this node as an ingredient of its parent — the lesser of buying
    /// it off the trading post or crafting it from its own ingredients — or <c>null</c> when neither is
    /// known. Following gw2efficiency's model, a missing trading-post price is treated as <em>unknown</em>,
    /// never as 0: a base item with no buy order and no recipe has an unknowable cost, and a craftable
    /// item with no buy order must be costed at its craft cost. (gw2efficiency additionally prices known
    /// vendor items from a static table; we don't have one yet, so such items read as unknown here.)
    /// </summary>
    public decimal? EffectiveCostOrNull
    {
        get
        {
            decimal? buy = BuyPricePerUnit > 0 ? BuyPrice : null;

            if (!IsCraftable)
            {
                return buy;
            }

            decimal craftSum = 0m;
            bool craftKnown = true;
            foreach (RecipeNode child in Ingredients)
            {
                if (child.IsCurrency)
                {
                    continue;
                }

                decimal? childCost = child.EffectiveCostOrNull;
                if (childCost is null)
                {
                    craftKnown = false;
                    break;
                }

                craftSum += childCost.Value;
            }

            decimal? craft = craftKnown ? craftSum : null;
            return (craft, buy) switch
            {
                (null, _) => buy,
                (_, null) => craft,
                _ => Math.Min(craft.Value, buy.Value)
            };
        }
    }

    /// <summary>
    /// The cost used to roll a node up into its parent. Unlike <see cref="EffectiveCostOrNull"/> (which is
    /// all-or-nothing — a single unpriced ingredient makes the whole craft unknown), this treats untradeable
    /// or unpriced parts as 0 and <em>sums the rest</em>, so a recipe with one account-bound input still
    /// contributes its tradeable parts instead of collapsing to 0. A fully-priced node still takes the cheaper
    /// of crafting vs. buying; a partially-priced node prefers a real buy order (a true upper bound) and falls
    /// back to the partial craft only when the item can't be bought at all (untradeable).
    /// </summary>
    public decimal EffectiveCost
    {
        get
        {
            decimal? buy = BuyPricePerUnit > 0 ? BuyPrice : null;
            if (!IsCraftable)
            {
                return buy ?? 0m;
            }

            decimal craftSum = 0m;
            var craftFullyKnown = true;
            foreach (RecipeNode child in Ingredients)
            {
                if (child.IsCurrency)
                {
                    continue;
                }

                craftSum += child.EffectiveCost; // untradeable/unpriced children contribute 0
                if (child.EffectiveCostOrNull is null)
                {
                    craftFullyKnown = false;
                }
            }

            if (craftFullyKnown)
            {
                return buy is { } known ? Math.Min(known, craftSum) : craftSum;
            }

            // Partially priced: the craft sum is only a lower bound, so prefer a known buy order; fall back to
            // the partial craft only when the item is untradeable (no buy order to defer to).
            return buy ?? craftSum;
        }
    }

    /// <summary>
    /// True when this node can be crafted and every ingredient on its cheapest path has a known cost. The
    /// profitable feed drops recipes where this is false, rather than reporting a cost built on a base
    /// item priced at 0.
    /// </summary>
    public bool CraftCostKnown =>
        IsCraftable && Ingredients.Where(child => !child.IsCurrency).All(child => child.EffectiveCostOrNull is not null);
}
