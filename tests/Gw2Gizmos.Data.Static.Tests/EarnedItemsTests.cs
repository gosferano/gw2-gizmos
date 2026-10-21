using Gw2Gizmos.Data.Static.Crafting;

namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>
/// The items kept as earned by playing are only that: each says how it's earned, and none is also made by a static
/// recipe, which would give it a real cost the engine should use instead.
/// </summary>
public sealed class EarnedItemsTests
{
    [Fact]
    public void Every_earned_item_says_how_its_earned()
    {
        Assert.All(EarnedItems.ByItemId.Values, how => Assert.False(string.IsNullOrWhiteSpace(how)));
    }

    [Fact]
    public void No_earned_item_is_also_made_by_a_static_recipe()
    {
        Assert.All(EarnedItems.ByItemId.Keys, id =>
            Assert.False(StaticRecipes.ByOutputItemId.ContainsKey(id), $"Item {id} has a static recipe."));
    }
}
