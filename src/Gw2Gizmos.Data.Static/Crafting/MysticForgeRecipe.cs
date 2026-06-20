namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// A Mystic Forge recipe scraped from the wiki (the GW2 API's <c>/v2/recipes</c> doesn't include forge
/// recipes). Carries display names and the wiki source page alongside ids, so the UI can show how an item is
/// forged and where the recipe comes from. <see cref="OutputId"/>/<see cref="MysticForgeIngredient.Id"/> are
/// null for the few names that don't map to a single game item (e.g. "Ascended armor").
/// </summary>
public sealed record MysticForgeRecipe
{
    public string Output { get; init; } = "";

    public int? OutputId { get; init; }

    /// <summary>Low end of the output yield. Both this and <see cref="OutputCountUpper"/> are 1 for an ordinary
    /// fixed-yield recipe; they differ only for random-yield recipes (Mystic Forge material promotions output a
    /// range, e.g. Mithril Ore yields 40–200).</summary>
    public int OutputCountLower { get; init; } = 1;

    /// <summary>High end of the output yield; equals <see cref="OutputCountLower"/> for a fixed-yield recipe.</summary>
    public int OutputCountUpper { get; init; } = 1;

    /// <summary>Probability that a forge actually yields the output — 1.0 for an ordinary guaranteed recipe.
    /// Some recipes only have a <em>chance</em> to succeed (Mystic Clover ~0.31); the wiki states this in prose,
    /// not a parseable field, so known values are filled in by <see cref="MysticForgeRecipes"/> after load.</summary>
    public double Chance { get; init; } = 1.0;

    /// <summary>Expected (average) yield: the midpoint of the [lower, upper] range, scaled by <see cref="Chance"/>.
    /// This is the count to cost a recipe against — a single forge gives a random amount (and may fail), but over
    /// many forges the per-output cost converges on this. Equals the fixed count for a guaranteed single-output
    /// recipe.</summary>
    public double ExpectedOutputCount => (OutputCountLower + OutputCountUpper) / 2.0 * Chance;

    public IReadOnlyList<MysticForgeIngredient> Ingredients { get; init; } = [];

    /// <summary>The wiki page the recipe was scraped from (for attribution / a "view on wiki" link).</summary>
    public string SourcePage { get; init; } = "";
}

/// <summary>One input of a <see cref="MysticForgeRecipe"/>: a count and the item's name + id.</summary>
public sealed record MysticForgeIngredient
{
    public int Count { get; init; }

    public string Name { get; init; } = "";

    public int? Id { get; init; }
}
