namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// A crafting recipe that the GW2 API's <c>/v2/recipes</c> doesn't expose — Mystic Forge combines, daily
/// "Place of Power" crafts (e.g. Charged Quartz Crystal), and other special recipes. The recipe engine falls
/// back to these when an item has no API recipe, so an otherwise account-bound intermediate can still be
/// priced from its tradeable inputs instead of counting as 0.
/// </summary>
public sealed record StaticRecipe
{
    /// <summary>The item this recipe produces.</summary>
    public required int OutputItemId { get; init; }

    /// <summary>How many of <see cref="OutputItemId"/> a single craft yields (usually 1).</summary>
    public int OutputItemCount { get; init; } = 1;

    /// <summary>What the craft consumes. Reference tradeable items where possible so the cost is priceable.</summary>
    public required IReadOnlyList<StaticIngredient> Ingredients { get; init; }
}

/// <summary>One input of a <see cref="StaticRecipe"/>: an item id and how many are consumed per craft.</summary>
public sealed record StaticIngredient
{
    public required int ItemId { get; init; }

    public required int Count { get; init; }
}
