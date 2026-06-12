using System.CommandLine;
using ClosedXML.Excel;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;

// Define options
var connectionStringArgument = new Argument<string>("connection") { Description = "SQLite connection string" };
var outputFilePathArgument = new Argument<string>("outputFilePath")
{
    Description = "Path to save the output file (optional, default: console output)",
    Arity = ArgumentArity.ExactlyOne
};
var sellPriceOption = new Option<PriceType>("--sell-price")
{
    Description = "Price type for selling items (default: BuyOrder)",
    DefaultValueFactory = _ => PriceType.BuyOrder, // Use buy orders when selling (instant sale)
};
var buyPriceOption = new Option<PriceType>("--buy-price")
{
    Description = "Price type for buying items (default: SellOrder)",
    DefaultValueFactory = _ => PriceType.SellOrder, // Use sell orders when buying (instant buy)
};

// Create the root command
var rootCommand = new RootCommand("Build and display crafting recipe tree")
{
    connectionStringArgument,
    outputFilePathArgument,
    sellPriceOption,
    buyPriceOption,
};

rootCommand.SetAction(
    async (parseResult, ct) =>
    {
        string connectionString = parseResult.GetRequiredValue(connectionStringArgument);
        string outputFilePath = parseResult.GetRequiredValue(outputFilePathArgument);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var configuration = new Configuration
        {
            BuyPriceType = parseResult.GetValue(buyPriceOption),
            SellPriceType = parseResult.GetValue(sellPriceOption)
        };

        var recipeTrees = await GetRecipeTrees(configuration, connectionString, ct);

        // Write to XLSX
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Recipe Trees");

        worksheet.Cell(1, 1).Value = "OutputItemId";
        worksheet.Cell(1, 2).Value = "OutputItemName";
        worksheet.Cell(1, 3).Value = "CraftingPrice";
        worksheet.Cell(1, 4).Value = "BuyPrice";
        worksheet.Cell(1, 5).Value = "SellPrice";
        worksheet.Cell(1, 6).Value = "CraftingTree";
        worksheet.Cell(1, 7).Value = "BuyingPlan";
        worksheet.Cell(1, 8).Value = "CraftingPlan";

        worksheet.Column(1).AdjustToContents();
        worksheet.Column(2).AdjustToContents();
        worksheet.Column(3).AdjustToContents();
        worksheet.Column(4).AdjustToContents();
        worksheet.Column(5).AdjustToContents();
        worksheet.Column(6).AdjustToContents();
        worksheet.Column(7).AdjustToContents();
        worksheet.Column(8).AdjustToContents();

        foreach (RecipeNode recipeNode in recipeTrees)
        {
            IXLRow lastRowUsed = worksheet.LastRowUsed()!;
            int lastRowNumber = lastRowUsed.RowNumber();
            int currentRow = lastRowNumber + 1;

            string craftingTree = RecipeTreeDisplay.GetCraftingAndBuyingPlan(recipeNode);
            string buyingPlan = RecipeTreeDisplay.GetFlattenedStructure(recipeNode);
            string craftingPlan = RecipeTreeDisplay.GetCraftingPlan(recipeNode);

            worksheet.Cell(currentRow, 1).Value = recipeNode.ItemId;
            worksheet.Cell(currentRow, 2).Value = recipeNode.ItemName;
            worksheet.Cell(currentRow, 3).Value = recipeNode.CraftingCost.ToString("F2");
            worksheet.Cell(currentRow, 4).Value = recipeNode.BuyPrice.ToString("F2");
            worksheet.Cell(currentRow, 5).Value = recipeNode.SellPrice.ToString("F2");
            worksheet.Cell(currentRow, 6).Value = craftingTree;
            worksheet.Cell(currentRow, 7).Value = buyingPlan;
            worksheet.Cell(currentRow, 8).Value = craftingPlan;

            lastRowUsed.AdjustToContents();
        }

        workbook.SaveAs(outputFilePath);

        stopwatch.Stop();
        Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms");
    }
);

ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static async Task<RecipeNode[]> GetRecipeTrees(
    Configuration configuration,
    string connectionString,
    CancellationToken ct
)
{
    await using var dbContext = new Gw2GizmosDbContext(
        new DbContextOptionsBuilder<Gw2GizmosDbContext>().UseSqlite(connectionString).Options
    );

    var recipeTreeBuilder = new RecipeTreeBuilder(dbContext, configuration);

    var result = await recipeTreeBuilder.GetRecipeTrees(ct);
    return result.ToArray();
}
