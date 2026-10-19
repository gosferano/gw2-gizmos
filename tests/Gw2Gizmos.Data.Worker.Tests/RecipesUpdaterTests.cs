using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>The recipes sync stores /v2/recipes and brings recipes it has seen before up to date.</summary>
public sealed class RecipesUpdaterTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();
    private readonly RoutingHttpStub _stub = new();
    private readonly ServiceProvider _provider;

    public RecipesUpdaterTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<Gw2GizmosDbContext>(o => new SqliteDbProvider().Configure(o, _fixture.ConnectionString));
        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _fixture.Dispose();
    }

    private async Task SyncAsync(params string[] recipes)
    {
        _stub.SetJson("/v2/recipes", "[1, 2]");
        _stub.SetJson("/v2/recipes?ids=1,2", $"[{string.Join(", ", recipes)}]");
        var updater = new RecipesUpdater(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _stub.BuildClientFactory(),
            NullLogger<RecipesUpdater>.Instance
        );
        await updater.UpdateRecipes(CancellationToken.None);
    }

    private static string RecipeJson(int id, params (int ItemId, int Count)[] ingredients)
    {
        string list = string.Join(
            ", ",
            ingredients.Select(i => $$"""{"type": "Item", "id": {{i.ItemId}}, "count": {{i.Count}}}""")
        );
        return $$"""
            {
              "id": {{id}}, "type": "Refinement", "output_item_id": {{1000 + id}}, "output_item_count": 1,
              "time_to_craft_ms": 1000, "disciplines": ["Artificer"], "min_rating": 0, "flags": ["AutoLearned"],
              "ingredients": [{{list}}], "chat_link": "[&CQEAAAA=]"
            }
            """;
    }

    private List<RecipeIngredient> IngredientsOf(int recipeId) =>
        _fixture
            .NewContext()
            .RecipeIngredients.AsNoTracking()
            .Where(ingredient => ingredient.RecipeId == recipeId)
            .OrderBy(ingredient => ingredient.Id)
            .ToList();

    [Fact]
    public async Task Updates_recipes_it_has_seen_before_including_their_ingredients()
    {
        await SyncAsync(RecipeJson(1, (10, 1), (11, 2)), RecipeJson(2, (10, 3), (12, 1)));

        // Unchanged, changed count, removed and added ingredients — on recipes that already exist.
        await SyncAsync(RecipeJson(1, (10, 1), (11, 5)), RecipeJson(2, (10, 3), (13, 4)));

        Assert.Equal([(10, 1), (11, 5)], IngredientsOf(1).Select(i => (i.Id, i.Count)));
        Assert.Equal([(10, 3), (13, 4)], IngredientsOf(2).Select(i => (i.Id, i.Count)));
        Assert.Equal(2, await _fixture.NewContext().Recipes.CountAsync());
    }

    [Fact]
    public async Task Takes_a_recipe_or_ingredient_listed_twice_once()
    {
        await SyncAsync(RecipeJson(1, (10, 1)), RecipeJson(2, (12, 1)));

        await SyncAsync(RecipeJson(1, (10, 2)), RecipeJson(1, (10, 2)), RecipeJson(2, (12, 3), (12, 3)));

        Assert.Equal([(10, 2)], IngredientsOf(1).Select(i => (i.Id, i.Count)));
        Assert.Equal([(12, 3)], IngredientsOf(2).Select(i => (i.Id, i.Count)));
    }
}
