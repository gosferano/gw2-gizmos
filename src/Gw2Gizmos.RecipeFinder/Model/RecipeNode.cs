namespace Gw2Gizmos.RecipeFinder.Model;

public class RecipeNode
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public int SellPricePerUnit { get; set; }
    public int BuyPricePerUnit { get; set; }
    public decimal CraftingCostPerUnit { get; set; }
    public int Count { get; set; }
    public int OutputItemCount { get; set; } = 1;
    public bool IsCurrency { get; set; }
    public List<RecipeNode> Ingredients { get; set; } = new();
    public bool IsCraftable => Ingredients.Count > 0;

    public int SellPrice => SellPricePerUnit * Count;
    public int BuyPrice => BuyPricePerUnit * Count;
    public decimal CraftingCost => CraftingCostPerUnit * Count;
    public bool IsProfitable =>
        CraftingCostPerUnit > 0 && (CraftingCostPerUnit < BuyPricePerUnit || SellPricePerUnit == 0);

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

    /// <summary>Cheapest known acquisition cost, or 0 when unknown — used for cost roll-up in the builder.</summary>
    public decimal EffectiveCost => EffectiveCostOrNull ?? 0m;

    /// <summary>
    /// True when this node can be crafted and every ingredient on its cheapest path has a known cost. The
    /// profitable feed drops recipes where this is false, rather than reporting a cost built on a base
    /// item priced at 0.
    /// </summary>
    public bool CraftCostKnown =>
        IsCraftable && Ingredients.Where(child => !child.IsCurrency).All(child => child.EffectiveCostOrNull is not null);
}
