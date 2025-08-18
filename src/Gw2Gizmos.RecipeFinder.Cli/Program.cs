using System.CommandLine;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.RecipeFinder.Cli;
using Gw2Gizmos.RecipeFinder.Cli.Model;
using Gw2Gizmos.RecipeFinder.Cli.Services;
using Microsoft.EntityFrameworkCore;

// Define options
var connectionStringArgument = new Argument<string>("connection") { Description = "SQLite connection string" };
var outputItemIdArgument = new Argument<int>("outputItemId") { Description = "Output item ID to filter recipes" };

// Create the root command
var rootCommand = new RootCommand("Build and display crafting recipe tree")
{
    connectionStringArgument,
    outputItemIdArgument
};

rootCommand.SetAction(
    async (parseResult, ct) =>
    {
        string connectionString = parseResult.GetRequiredValue(connectionStringArgument);
        int outputItemId = parseResult.GetRequiredValue(outputItemIdArgument);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await BuildCraftingTree(connectionString, outputItemId, ct);

        await GetMostProfitableRecipesAsync(connectionString, ct);
        stopwatch.Stop();
        Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms");
    }
);

ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static async Task GetMostProfitableRecipesAsync(string connectionString, CancellationToken ct)
{
    await using var dbContext = new Gw2GizmosDbContext(
        new DbContextOptionsBuilder<Gw2GizmosDbContext>().UseSqlite(connectionString).Options
    );

    var recipeService = new RecipeService(dbContext);
    var itemService = new ItemService(dbContext);
    var priceService = new PriceService(dbContext);
    var recipeTreeBuilder = new RecipeTreeBuilder(recipeService, itemService, priceService);

    var recipeTrees = await recipeTreeBuilder.GetRecipeTrees(ct);

    var mostProfitableRecipes = recipeTreeBuilder.GetMostProfitableRecipesAsync(recipeTrees, 10);
    foreach (RecipeNode recipeTree in mostProfitableRecipes)
    {
        Display(recipeTree);
    }

    var mostProfitablePercentageRecipes = recipeTreeBuilder.GetMostProfitablePercentageRecipesAsync(recipeTrees, 10);
    foreach (RecipeNode recipeTree in mostProfitablePercentageRecipes)
    {
        Display(recipeTree);
    }
}

static async Task BuildCraftingTree(string connectionString, int outputItemId, CancellationToken ct)
{
    await using var dbContext = new Gw2GizmosDbContext(
        new DbContextOptionsBuilder<Gw2GizmosDbContext>().UseSqlite(connectionString).Options
    );

    var recipeService = new RecipeService(dbContext);
    var itemService = new ItemService(dbContext);
    var priceService = new PriceService(dbContext);
    var recipeTreeBuilder = new RecipeTreeBuilder(recipeService, itemService, priceService);

    RecipeNode recipeTree = await recipeTreeBuilder.BuildTreeAsync(outputItemId, ct);
    Display(recipeTree);
}

static void Display(RecipeNode recipeTree)
{
    string display = RecipeTreeDisplay.GetCraftingAndBuyingPlan(recipeTree);
    Console.WriteLine(display);
    string flattenedDisplay = RecipeTreeDisplay.GetFlattenedStructure(recipeTree);
    Console.WriteLine(flattenedDisplay);
}
