using System.Collections.Concurrent;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.Static.Crafting;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.RecipeFinder;

public class RecipeTreeBuilder
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Configuration _priceConfiguration;

    public RecipeTreeBuilder(Gw2GizmosDbContext dbContext, Configuration priceConfiguration)
    {
        _dbContext = dbContext;
        _priceConfiguration = priceConfiguration;
    }

    // Everything a tree walk needs, pulled into memory once. Tree building otherwise does ~3 DB round-trips per
    // node (price, name, recipes); a big tree (a legendary with promotion sub-chains) is hundreds of sequential
    // queries. After loading, the builder needs no DbContext — so the desktop loads once and reuses the builder
    // (memo and all) across selections; subsequent trees are pure in-memory.
    private Dictionary<int, List<Recipe>>? _recipesByOutput;
    private Dictionary<int, string>? _itemNames;
    private Dictionary<int, string>? _currencyNames;
    private Dictionary<int, string>? _currencyIconById;
    private Dictionary<string, string>? _currencyIconByName; // keyed by normalized name (singular/plural tolerant)
    private Dictionary<int, (int Buy, int Sell)>? _latestPrices;
    private Dictionary<string, decimal>? _currencyWeights;   // account currency name -> derived copper-per-unit

    /// <summary>Supply the latest (buy, sell) prices the caller has already computed (the Items grid does this
    /// once), so the builder skips re-running the heavy latest-price aggregation over the snapshot table.</summary>
    public void UseLatestPrices(Dictionary<int, (int Buy, int Sell)> latestPrices) => _latestPrices = latestPrices;

    public async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_recipesByOutput is null)
        {
            List<Recipe> recipes = await _dbContext.Recipes.AsNoTracking().Include(r => r.Ingredients).ToListAsync(ct);
            _recipesByOutput = recipes
                .GroupBy(recipe => recipe.OutputItemId)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        _itemNames ??= await _dbContext.Items.AsNoTracking()
            .Select(item => new { item.Id, item.Name })
            .ToDictionaryAsync(item => item.Id, item => item.Name, ct);

        if (_currencyNames is null)
        {
            var currencies = await _dbContext.Currencies.AsNoTracking()
                .Select(currency => new { currency.Id, currency.Name, currency.Icon })
                .ToListAsync(ct);
            _currencyNames = currencies.ToDictionary(c => c.Id, c => c.Name);
            _currencyIconById = currencies.Where(c => !string.IsNullOrEmpty(c.Icon)).ToDictionary(c => c.Id, c => c.Icon);
            // Normalized name → icon, so a scraped cost currency resolves even when its name differs from the API's
            // (e.g. "Tale of Dungeon Delving" vs "Tales of Dungeon Delving"). Last write wins on collisions.
            _currencyIconByName = currencies
                .Where(c => !string.IsNullOrEmpty(c.Icon))
                .GroupBy(c => NormalizeCurrencyName(c.Name))
                .ToDictionary(g => g.Key, g => g.First().Icon);
        }

        if (_latestPrices is null || _currencyWeights is null)
        {
            // Latest snapshot per item, fetched once for both the (buy, sell) price map and the currency
            // weights (which also need supply, for the liquidity gate). The grid may already have supplied the
            // price map via UseLatestPrices; the weights still need this pass (run in the background warm-up).
            IQueryable<long> latestIds = _dbContext.PriceSnapshots
                .GroupBy(snapshot => snapshot.ItemId)
                .Select(group => group.Max(snapshot => snapshot.Id));
            var latest = await _dbContext.PriceSnapshots.AsNoTracking()
                .Where(snapshot => latestIds.Contains(snapshot.Id))
                .Select(snapshot => new { snapshot.ItemId, snapshot.Buy, snapshot.Sell, snapshot.Supply })
                .ToListAsync(ct);

            _latestPrices ??= latest.ToDictionary(row => row.ItemId, row => (row.Buy ?? 0, row.Sell ?? 0));

            if (_currencyWeights is null)
            {
                var sellSupply = latest.ToDictionary(row => row.ItemId, row => (row.Sell ?? 0, row.Supply));
                _currencyWeights = CurrencyValuer
                    .DeriveWeights(itemId => sellSupply.GetValueOrDefault(itemId))
                    .ToDictionary(weight => weight.Currency, weight => weight.CopperPerUnit, StringComparer.Ordinal);
            }
        }

        // Force the embedded static data (vendor catalog ~22 MB JSON, forge recipes) to parse now — it
        // initializes lazily on first access, which would otherwise land on the first BuildTree (a UI click)
        // and stall it ~0.5s. Doing it here keeps that cost in the background warm-up.
        _ = VendorItems.ByItemId.Count + StaticRecipes.ByOutputItemId.Count;
    }

    public async Task<List<RecipeNode>> GetRecipeTrees(CancellationToken stoppingToken)
    {
        await EnsureLoadedAsync(stoppingToken);

        // Roots = every item any recipe produces: API recipes plus the static (Mystic Forge / Place-of-Power)
        // recipes, so forge-only outputs like legendaries are priced too, not just API-craftable items.
        var outputItemIds = _recipesByOutput!.Keys
            .Concat(StaticRecipes.ByOutputItemId.Keys)
            .Distinct()
            .ToList();

        var recipeTrees = new List<RecipeNode>();
        for (var i = 0; i < outputItemIds.Count; i++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            // Report progress every 100 items
            if (i % 100 == 0)
            {
                Console.WriteLine($"Processing recipe {i + 1}/{outputItemIds.Count}");
            }

            RecipeNode rootNode = await BuildTreeAsync(outputItemIds[i], stoppingToken);

            recipeTrees.Add(rootNode);
        }

        return recipeTrees;
    }

    public List<RecipeNode> GetMostProfitableRecipesAsync(List<RecipeNode> recipes, int topCount)
    {
        // Sort by profit margin (SellPriceAfterFee - CraftingCostWithFee) and take the top results
        return recipes
            .OrderByDescending(node => (node.SellPrice * 0.85m) - (node.CraftingCost))
            .Take(topCount)
            .ToList();
    }

    public List<RecipeNode> GetMostProfitablePercentageRecipesAsync(List<RecipeNode> recipes, int topCount)
    {
        // Sort by profitability percentage and take the top results
        return recipes
            .OrderByDescending(node => node.IsProfitable ? (node.SellPrice * 0.85m) / node.CraftingCost : 0)
            .Take(topCount)
            .ToList();
    }

    /// <param name="path">Item ids on the current branch from the root, used to detect recipe cycles (e.g. a
    /// material-promotion loop) — an item that is its own ancestor is priced as a leaf instead of recursing.
    /// This bounds every branch to the distinct item count, so recursion always terminates.</param>
    public async Task<RecipeNode> BuildTreeAsync(
        int rootItemId, CancellationToken ct, long parentMultiplier = 1, HashSet<int>? path = null)
    {
        await EnsureLoadedAsync(ct);
        bool isRoot = path is null;
        path ??= new HashSet<int>();
        var rootNode = new RecipeNode { ItemId = rootItemId, Count = parentMultiplier };
        var stack = new Stack<(RecipeNode Node, bool Processed)>();
        stack.Push((rootNode, false));

        while (stack.Count > 0)
        {
            var (currentNode, processed) = stack.Pop();

            if (processed)
            {
                // Calculate crafting cost per unit after processing all children
                if (currentNode.Ingredients.Count > 0)
                {
                    currentNode.CraftingCostPerUnit =
                        currentNode.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost)
                        / currentNode.Count;
                }

                _memoizationCache.TryAdd(currentNode.ItemId, CopyForMemo(currentNode, 1));
                continue;
            }

            // CHECK CACHE FIRST - before any expensive operations
            if (_memoizationCache.TryGetValue(currentNode.ItemId, out var cachedNode))
            {
                var scaledNode = CopyForMemo(cachedNode, currentNode.Count);

                currentNode.ItemName = scaledNode.ItemName;
                currentNode.SellPricePerUnit = scaledNode.SellPricePerUnit;
                currentNode.BuyPricePerUnit = scaledNode.BuyPricePerUnit;
                currentNode.CraftingCostPerUnit = scaledNode.CraftingCostPerUnit;
                currentNode.OutputItemCount = scaledNode.OutputItemCount;
                currentNode.IsVendorAcquirable = scaledNode.IsVendorAcquirable;
                currentNode.VendorOffers = scaledNode.VendorOffers;
                currentNode.VendorCoinCostPerUnit = scaledNode.VendorCoinCostPerUnit;
                currentNode.Ingredients = scaledNode.Ingredients;
                continue;
            }

            // Fetch prices
            TradingPostPrices tradingPostPrices = GetPrices(currentNode.ItemId);
            currentNode.BuyPricePerUnit =
                _priceConfiguration.BuyPriceType == PriceType.BuyOrder
                    ? tradingPostPrices.BuyOrderPrice
                    : tradingPostPrices.SellOrderPrice;
            currentNode.SellPricePerUnit =
                _priceConfiguration.SellPriceType == PriceType.BuyOrder
                    ? tradingPostPrices.BuyOrderPrice
                    : tradingPostPrices.SellOrderPrice;

            // Obtainable from any vendor (for any currency), so a coin-less untradeable item still counts as
            // acquirable rather than genuinely unpriceable. CopperPriceFor already folded coin vendors into price.
            currentNode.IsVendorAcquirable = VendorItems.IsAcquirable(currentNode.ItemId);
            AttachVendorCost(currentNode);

            // Fetch item name with fallback
            currentNode.ItemName = GetItemName(currentNode.ItemId);

            // Cycle: this item is its own ancestor (e.g. a material-promotion loop). Stop and leave it as a
            // trading-post-priced leaf so the tree is finite. Not memoized — its value is path-dependent (it
            // assumes the recursive copy is bought, not re-crafted).
            if (path.Contains(currentNode.ItemId))
            {
                continue;
            }

            path.Add(currentNode.ItemId);
            try
            {
                // All recipes for this item (preloaded; no per-node query).
                List<Recipe> recipes = _recipesByOutput!.GetValueOrDefault(currentNode.ItemId) ?? [];

                if (recipes is { Count: > 0 })
                {
                    RecipeNode? bestRecipeTree = null;
                    var lowestCraftingCost = decimal.MaxValue;

                    foreach (Recipe recipe in recipes)
                    {
                        // Build a separate tree for each recipe
                        RecipeNode recipeTree = await BuildRecipeTreeForComparison(recipe, currentNode.Count, ct, path);

                        if (recipeTree.CraftingCostPerUnit < lowestCraftingCost)
                        {
                            lowestCraftingCost = recipeTree.CraftingCostPerUnit;
                            bestRecipeTree = recipeTree;
                        }
                    }

                    if (bestRecipeTree != null)
                    {
                        // Copy the best recipe's data to current node
                        currentNode.OutputItemCount = bestRecipeTree.OutputItemCount;
                        currentNode.CraftingCostPerUnit = bestRecipeTree.CraftingCostPerUnit;
                        currentNode.Ingredients = bestRecipeTree.Ingredients;

                        // Mark as processed since we've built the complete subtree
                        _memoizationCache.TryAdd(currentNode.ItemId, CopyForMemo(currentNode, 1));
                    }
                }
                else if (StaticRecipes.ByOutputItemId.TryGetValue(currentNode.ItemId, out IReadOnlyList<StaticRecipe>? staticRecipes))
                {
                    // No API recipe, but hardcoded ones (Mystic Forge / daily craft) — price each from its inputs
                    // and keep the cheapest, so a forge-only intermediate isn't a 0-cost leaf and we don't have to
                    // guess which forge variant is best (e.g. the 1- vs 10-forge Mystic Clover).
                    RecipeNode? bestRecipeTree = null;
                    var lowestCraftingCost = decimal.MaxValue;
                    foreach (StaticRecipe staticRecipe in staticRecipes)
                    {
                        RecipeNode recipeTree = await BuildStaticRecipeTree(staticRecipe, currentNode.Count, ct, path);
                        if (recipeTree.CraftingCostPerUnit < lowestCraftingCost)
                        {
                            lowestCraftingCost = recipeTree.CraftingCostPerUnit;
                            bestRecipeTree = recipeTree;
                        }
                    }

                    if (bestRecipeTree != null)
                    {
                        currentNode.OutputItemCount = bestRecipeTree.OutputItemCount;
                        currentNode.CraftingCostPerUnit = bestRecipeTree.CraftingCostPerUnit;
                        currentNode.Ingredients = bestRecipeTree.Ingredients;
                        _memoizationCache.TryAdd(currentNode.ItemId, CopyForMemo(currentNode, 1));
                    }
                }
                else
                {
                    // Leaf node - no recipe, mark as processed
                    stack.Push((currentNode, true));
                }
            }
            finally
            {
                path.Remove(currentNode.ItemId);
            }
        }

        // Stamp depth on the finished root tree so the UI can expand only the top levels (deep promotion chains
        // would otherwise render tens of thousands of nodes at once and freeze).
        if (isRoot)
        {
            SetDepth(rootNode, 0);
        }

        return rootNode;
    }

    private static void SetDepth(RecipeNode node, int depth)
    {
        node.Depth = depth;
        foreach (RecipeNode child in node.Ingredients)
        {
            SetDepth(child, depth + 1);
        }
    }

    private readonly ConcurrentDictionary<int, RecipeNode> _memoizationCache = new();

    private RecipeNode CopyForMemo(RecipeNode node, long targetCount)
    {
        // Calculate crafts needed for both cached node and target (whole forges; yield may be fractional)
        long cachedCraftsNeeded = (long)Math.Ceiling(node.Count / node.OutputItemCount);
        long targetCraftsNeeded = (long)Math.Ceiling(targetCount / node.OutputItemCount);
        double scalingFactor = (double)targetCraftsNeeded / cachedCraftsNeeded;

        return new RecipeNode
        {
            ItemId = node.ItemId,
            ItemName = node.ItemName,
            SellPricePerUnit = node.SellPricePerUnit,
            BuyPricePerUnit = node.BuyPricePerUnit,
            CraftingCostPerUnit = node.CraftingCostPerUnit,
            Count = targetCount,
            OutputItemCount = node.OutputItemCount,
            IsVendorAcquirable = node.IsVendorAcquirable,
            VendorOffers = node.VendorOffers,
            VendorCoinCostPerUnit = node.VendorCoinCostPerUnit,
            Ingredients = node
                .Ingredients.Select(ingredient =>
                {
                    var scaledCount = (long)Math.Round(ingredient.Count * scalingFactor);
                    return CopyForMemo(ingredient, scaledCount);
                })
                .ToList()
        };
    }

    private async Task<RecipeNode> BuildRecipeTreeForComparison(
        Recipe recipe, long targetCount, CancellationToken ct, HashSet<int> path)
    {
        var recipeNode = new RecipeNode
        {
            ItemId = recipe.OutputItemId,
            Count = targetCount,
            OutputItemCount = recipe.OutputItemCount
        };

        long recipeCraftsNeeded = (targetCount + recipe.OutputItemCount - 1) / recipe.OutputItemCount;
        decimal producedCount = recipeCraftsNeeded * (decimal)recipe.OutputItemCount;

        // Build ingredient trees recursively
        foreach (var ingredient in recipe.Ingredients)
        {
            long requiredCount = ingredient.Count * recipeCraftsNeeded;
            if (requiredCount == 0)
                requiredCount = 1;

            RecipeNode ingredientTree;

            // Check if this ingredient is a currency
            if (ingredient.Type == "Currency")
            {
                ingredientTree = new RecipeNode
                {
                    ItemId = ingredient.Id,
                    Count = requiredCount,
                    IsCurrency = true,
                    BuyPricePerUnit = 0,
                    SellPricePerUnit = 0,
                    CraftingCostPerUnit = 0,
                    ItemName = GetCurrencyName(ingredient.Id)
                };
            }
            else
            {
                ingredientTree = await BuildTreeAsync(ingredient.Id, ct, requiredCount, path);
            }

            recipeNode.Ingredients.Add(ingredientTree);
        }

        // Cost per output = total ingredient cost / total produced (crafts × yield), not / target — so a craft
        // that makes more than asked (or, for random recipes, a fractional expected yield) is costed exactly.
        recipeNode.CraftingCostPerUnit =
            recipeNode.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost) / producedCount;

        return recipeNode;
    }

    /// <summary>
    /// Builds a subtree for a hardcoded <see cref="StaticRecipe"/> (one the API doesn't expose). Mirrors
    /// <see cref="BuildRecipeTreeForComparison"/>, but every ingredient is a real item (no currency inputs),
    /// so each recurses through <see cref="BuildTreeAsync"/> and is priced from the trading post / vendors.
    /// </summary>
    private async Task<RecipeNode> BuildStaticRecipeTree(
        StaticRecipe recipe, long targetCount, CancellationToken ct, HashSet<int> path)
    {
        var recipeNode = new RecipeNode
        {
            ItemId = recipe.OutputItemId,
            Count = targetCount,
            OutputItemCount = recipe.OutputItemCount
        };

        // Whole forges to cover the target (yield may be fractional for random/promotion recipes).
        long craftsNeeded = (long)Math.Ceiling(targetCount / recipe.OutputItemCount);
        decimal producedCount = craftsNeeded * recipe.OutputItemCount;

        foreach (StaticIngredient ingredient in recipe.Ingredients)
        {
            long requiredCount = ingredient.Count * craftsNeeded;
            if (requiredCount == 0)
            {
                requiredCount = 1;
            }

            recipeNode.Ingredients.Add(await BuildTreeAsync(ingredient.ItemId, ct, requiredCount, path));
        }

        // Cost per output = total ingredient cost / total produced (crafts × expected yield), so a fractional
        // expected yield (e.g. a Mystic Clover forge's ~3.1) is costed exactly, not rounded.
        recipeNode.CraftingCostPerUnit =
            recipeNode.Ingredients.Where(child => !child.IsCurrency).Sum(child => child.EffectiveCost) / producedCount;

        return recipeNode;
    }

    /// <summary>The best trading-post buy/sell unit prices for an item, from the preloaded latest-price map.</summary>
    private TradingPostPrices GetPrices(int itemId)
    {
        // Latest poll snapshot per item (preloaded). Not on the trading post → 0; a side of the book empty → 0.
        _latestPrices!.TryGetValue(itemId, out (int Buy, int Sell) latest);

        // Acquisition price (SellOrderPrice = lowest sell listing = what you pay): take the cheaper of the
        // trading post and a coin vendor that sells it. The vendor price both fills in untradeable items the
        // TP doesn't list and undercuts the TP where a vendor is cheaper. BuyOrderPrice stays TP-only.
        int tradingPostSell = latest.Sell;
        int? vendor = VendorItems.CopperPriceFor(itemId);
        int sellOrderPrice = (tradingPostSell, vendor) switch
        {
            ( > 0, int v) => Math.Min(tradingPostSell, v),
            (0, int v) => v,
            _ => tradingPostSell
        };

        return new TradingPostPrices(sellOrderPrice, latest.Buy);
    }

    /// <summary>Attach the vendor sale terms to a node: each distinct offer kept whole — its quantity and its
    /// full cost (every component together, amounts exactly as charged, each with its icon resolved). Offers are
    /// ordered simplest first (single-unit, then fewest components, then cheapest), so the first is shown inline
    /// and the rest on hover.</summary>
    private void AttachVendorCost(RecipeNode node)
    {
        if (!VendorItems.ByItemId.TryGetValue(node.ItemId, out VendorItem? vendorItem))
        {
            return;
        }

        node.VendorOffers = vendorItem.Offers
            .Where(offer => offer.Quantity > 0 && offer.Cost.Count > 0)
            .Select(offer => new Model.VendorOffer(
                offer.Quantity,
                offer.Cost
                    .Select(cost => new VendorCost(
                        cost.Value, cost.Currency, cost.ItemId, ResolveCurrencyIcon(cost), cost.CurrencyId == 1, cost.CurrencyId))
                    .ToList()))
            .GroupBy(offer => $"{offer.Quantity}|{string.Join('+', offer.Cost.Select(cost => $"{cost.Amount} {cost.Currency}"))}")
            .Select(group => group.First())
            // Cheapest coin-equivalent per unit first, so PrimaryVendorOffer is the best buy. Offers we can't
            // value (an unweighted account currency) sort last but stay listed.
            .OrderBy(offer => OfferPerUnitCoinValue(offer) ?? decimal.MaxValue)
            .ThenBy(offer => offer.Cost.Count)
            .ToList();

        // Per-unit coin-equivalent of the cheapest valued offer — what feeds the cost model (craft vs. buy and
        // the roll-up into parents); null when no offer could be valued.
        List<decimal> perUnit = node.VendorOffers
            .Select(OfferPerUnitCoinValue)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        node.VendorCoinCostPerUnit = perUnit.Count > 0 ? perUnit.Min() : null;
    }

    /// <summary>An offer's coin-equivalent per single unit it yields (its full valued cost ÷ its quantity), or
    /// null when a cost currency has no derived weight.</summary>
    private decimal? OfferPerUnitCoinValue(Model.VendorOffer offer) =>
        OfferValuer.CoinValue(offer, _currencyWeights!, itemId => _latestPrices!.GetValueOrDefault(itemId).Sell)
            is { } total
            ? total / offer.Quantity
            : null;

    /// <summary>The icon URL for a cost component's currency, or null for an item-currency (rendered via its item
    /// icon instead). Resolved by currency id, then by normalized name (singular/plural tolerant).</summary>
    private string? ResolveCurrencyIcon(CostComponent cost)
    {
        if (cost.ItemId is > 0)
        {
            return null; // an item-currency — the UI shows the item icon by id
        }

        if (cost.CurrencyId is { } id && _currencyIconById!.TryGetValue(id, out string? byId))
        {
            return byId;
        }

        return _currencyIconByName!.GetValueOrDefault(NormalizeCurrencyName(cost.Currency));
    }

    /// <summary>Normalize a currency name for matching: lower-cased, with a trailing 's' trimmed from each word,
    /// so "Tale of Dungeon Delving" and the API's "Tales of Dungeon Delving" map to the same key.</summary>
    private static string NormalizeCurrencyName(string name) =>
        string.Join(' ', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.EndsWith('s') ? word[..^1] : word));

    private string GetItemName(int itemId) =>
        _itemNames!.GetValueOrDefault(itemId) ?? $"Unknown Item ({itemId})";

    private string GetCurrencyName(int currencyId) =>
        _currencyNames!.GetValueOrDefault(currencyId) ?? $"Unknown Currency ({currencyId})";
}
