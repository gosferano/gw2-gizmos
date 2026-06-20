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

    // Expected yield per craft: 1 for ordinary recipes, but fractional for random-yield Mystic Forge recipes
    // (a Mystic Clover forge averages ~3.1, material promotions a wide range). Decimal, not rounded, so the
    // per-output craft cost stays exact.
    public decimal OutputItemCount { get; set; } = 1;

    /// <summary>Depth from the root (root = 0), set after the tree is built. Drives default expansion so a huge
    /// tree (a legendary's promotion chains) doesn't render every node at once and freeze the UI.</summary>
    public int Depth { get; set; }

    /// <summary>Whether this node is expanded by default in the craft tree — only the top couple of levels, so
    /// the rest renders lazily when the user expands it.</summary>
    public bool AutoExpand => Depth < 2;
    public bool IsCurrency { get; set; }

    /// <summary>Whether some NPC vendor sells this item for <em>any</em> currency (coin, karma, laurels, …), set
    /// by the builder from the vendor catalog. An uncraftable, untradeable item that's still vendor-acquirable is
    /// obtainable (just not coin-priced), so it counts as known-cost rather than genuinely unpriceable.</summary>
    public bool IsVendorAcquirable { get; set; }

    public List<RecipeNode> Ingredients { get; set; } = new();
    public bool IsCraftable => Ingredients.Count > 0;

    /// <summary>A direct ingredient is a genuine dead-end: a leaf (no recipe) with no obtainable price at all —
    /// no trading post, and no vendor in any currency (e.g. account-bound precursor-crafting parts like Spirit
    /// of the Perfected Nightsword or Essence of Gloom). The craft can't actually be performed, so this node
    /// should be valued at its own buy price instead.</summary>
    public bool HasUnobtainableDirectIngredient =>
        Ingredients.Any(child => !child.IsCurrency && !child.IsCraftable && child.EffectiveCostOrNull is null);

    /// <summary>Whether to show a craft cost for this node in the tree. False for a leaf (no recipe) and for a
    /// craftable whose craft can't be done because a direct ingredient is an unobtainable dead-end — that node
    /// "collapses to buy" (craft shown as an em-dash, valued at its buy price). Items whose unobtainable parts
    /// are buried deeper still show a craft estimate, so a legendary doesn't collapse just because a precursor
    /// component is account-bound.</summary>
    public bool ShowCraftCost => IsCraftable && !HasUnobtainableDirectIngredient;

    // long: deep trees (e.g. the Agony Infusion doubling chain) push Count into the millions, so a copper
    // total of pricePerUnit * Count easily exceeds int.MaxValue (~214,748g) and would wrap negative.
    public long SellPrice => (long)SellPricePerUnit * Count;
    public long BuyPrice => (long)BuyPricePerUnit * Count;
    public decimal CraftingCost => CraftingCostPerUnit * Count;

    /// <summary>The ways a vendor sells this item — each offer keeps its full cost (all components together) and
    /// quantity. Set by the builder, simplest/cheapest first; empty when no vendor sells it.</summary>
    public IReadOnlyList<VendorOffer> VendorOffers { get; set; } = [];

    /// <summary>The cheapest offer for one bundle, used to pick what to show.</summary>
    public VendorOffer? PrimaryVendorOffer => VendorOffers.Count > 0 ? VendorOffers[0] : null;

    /// <summary>The cheapest offer scaled to this node's whole required <see cref="Count"/> (the buy column shows
    /// the cost for all of them, not one unit — matching the coin buy price, which is per-Count).</summary>
    public VendorOffer? PrimaryVendorPurchase => ScaleToCount(PrimaryVendorOffer);

    /// <summary>Every offer scaled to the whole <see cref="Count"/>, for the hover list.</summary>
    public IReadOnlyList<VendorOffer> VendorPurchases => VendorOffers.Select(ScaleToCount).OfType<VendorOffer>().ToList();

    /// <summary>Scale an offer's cost to acquire the node's whole <see cref="Count"/>: the number of bundles
    /// needed (rounded up) times each component, with the offer's quantity set to that whole count.</summary>
    private VendorOffer? ScaleToCount(VendorOffer? offer)
    {
        if (offer is null)
        {
            return null;
        }

        long bundles = (Count + offer.Quantity - 1) / offer.Quantity;
        return new VendorOffer(Count, offer.Cost.Select(cost => cost with { Amount = cost.Amount * bundles }).ToList());
    }

    /// <summary>The buy column shows a coin amount: the item has a trading-post or coin-vendor price.</summary>
    public bool HasCoinBuyPrice => BuyPrice > 0;

    /// <summary>The buy column shows the vendor cost (amounts + currency icons) instead of coin: no coin/TP price,
    /// but a vendor sells it for a currency.</summary>
    public bool ShowVendorPrice => BuyPrice <= 0 && PrimaryVendorOffer is not null;

    /// <summary>The buy column shows an em-dash: the item is genuinely unpurchasable — no trading-post offer and
    /// no vendor in any currency (account-bound). A 0 would read as "free".</summary>
    public bool ShowBuyDash => !HasCoinBuyPrice && !ShowVendorPrice;

    /// <summary>Coin-equivalent of the cheapest <em>valued</em> vendor offer, per single unit (each currency
    /// amount × its derived weight). Set by the builder; null when no offer could be valued. This lets a vendor
    /// purchase compete with the trading post and crafting in the cost model — it does not change the displayed
    /// vendor amount + icon.</summary>
    public decimal? VendorCoinCostPerUnit { get; set; }

    /// <summary>The vendor coin-equivalent to acquire this node's whole <see cref="Count"/>.</summary>
    public decimal? VendorCoinCost => VendorCoinCostPerUnit is { } perUnit ? perUnit * Count : null;

    /// <summary>The cheapest coin-equivalent way to buy this node — a trading-post / coin-vendor price or a
    /// valued vendor offer — or null when neither is known.</summary>
    private decimal? CheapestAcquisition()
    {
        decimal? coin = BuyPrice > 0 ? BuyPrice : null;
        return (coin, VendorCoinCost) switch
        {
            (not null, { } vendor) => Math.Min(coin.Value, vendor),
            (not null, null) => coin,
            (null, { } vendor) => vendor,
            _ => null,
        };
    }

    public bool IsProfitable =>
        CraftingCostPerUnit > 0 && (CraftingCostPerUnit < BuyPricePerUnit || SellPricePerUnit == 0);

    /// <summary>Crafting is strictly cheaper (or the only available way) to obtain this node — bolded in the
    /// recipe tree. When craft and buy are equal, neither is bolded; a 0 on either side means that option
    /// isn't available rather than "free". A node that collapses to buy (<see cref="ShowCraftCost"/> false)
    /// never counts as craft-cheaper — its craft can't actually be performed.</summary>
    public bool CraftIsCheaper => ShowCraftCost && CraftingCost > 0 && (BuyPrice <= 0 || CraftingCost < BuyPrice);

    /// <summary>Buying is strictly cheaper (or the only available way) to obtain this node — bolded in the
    /// recipe tree. Equal craft/buy bolds neither. When the craft collapses to buy, the buy order is the value.</summary>
    public bool BuyIsCheaper => BuyPrice > 0 && (!ShowCraftCost || CraftingCost <= 0 || BuyPrice < CraftingCost);

    /// <summary>
    /// The cheapest known cost to obtain this node as an ingredient of its parent — the lesser of buying
    /// it off the trading post or crafting it from its own ingredients — or <c>null</c> when neither is
    /// known. A missing trading-post price is treated as <em>unknown</em>, never as 0 — with one exception: an
    /// uncraftable item a vendor sells for some non-coin currency (karma, laurels, …) is obtainable, so it reads
    /// as 0 coin rather than unknown. Only a genuinely unobtainable item (no trading post, no recipe, and not
    /// vendor-sold for any currency) is unknown.
    /// </summary>
    public decimal? EffectiveCostOrNull
    {
        get
        {
            decimal? acquire = CheapestAcquisition();

            if (!IsCraftable)
            {
                // Cheapest buy/vendor coin-equivalent; else 0 when a vendor sells it for a currency we couldn't
                // value (obtainable, just uncosted); else genuinely unknown (account-bound, no acquisition path).
                return acquire ?? (IsVendorAcquirable ? 0m : null);
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
            return (craft, acquire) switch
            {
                (null, _) => acquire,
                (_, null) => craft,
                _ => Math.Min(craft.Value, acquire.Value)
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
            decimal? acquire = CheapestAcquisition();
            if (!IsCraftable)
            {
                return acquire ?? 0m;
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
                return acquire is { } known ? Math.Min(known, craftSum) : craftSum;
            }

            // Partially priced: the craft sum is only a lower bound, so prefer a known buy/vendor cost; fall back
            // to the partial craft only when the item can't be bought at all.
            return acquire ?? craftSum;
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
