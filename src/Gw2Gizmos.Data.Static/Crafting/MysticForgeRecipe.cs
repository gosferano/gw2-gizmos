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

    public int OutputCount { get; init; } = 1;

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
