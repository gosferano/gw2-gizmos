using System.CommandLine;
using System.Data;
using System.Data.SQLite;
using ClosedXML.Excel;

// Define options
var connectionStringArgument = new Argument<string>("connection") { Description = "SQLite connection string" };

var outputFilePathArgument = new Argument<string>("output") { Description = "Output file path for the Excel file" };

// Create the root command
var rootCommand = new RootCommand("Export SQLite data to Excel") { connectionStringArgument, outputFilePathArgument };

rootCommand.SetAction(parseResult =>
{
    string connectionString = parseResult.GetRequiredValue(connectionStringArgument);
    string outputFilePath = parseResult.GetRequiredValue(outputFilePathArgument);

    ExportDataToExcel(connectionString, outputFilePath);
});

ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static void ExportDataToExcel(string sqliteConnectionString, string outputFilePath)
{
    using var connection = new SQLiteConnection(sqliteConnectionString);
    connection.Open();

    string[] tableNames = GetTableNames(connection);

    using (var workbook = new XLWorkbook())
    {
        foreach (string tableName in tableNames)
        {
            DataTable dataTable = GetTableData(connection, tableName);
            workbook.Worksheets.Add(dataTable, tableName);
        }

        workbook.SaveAs(outputFilePath);
    }

    Console.WriteLine($"Data exported to {outputFilePath}");
}

static DataTable GetTableData(SQLiteConnection connection, string tableName)
{
    var dataTable = new DataTable();
    using var command = new SQLiteCommand($"SELECT * FROM {tableName}", connection);
    using var adapter = new SQLiteDataAdapter(command);
    adapter.Fill(dataTable);
    return dataTable;
}

static string[] GetTableNames(SQLiteConnection connection)
{
    var tableNames = new DataTable();
    using (
        var command = new SQLiteCommand(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';",
            connection
        )
    )
    using (var adapter = new SQLiteDataAdapter(command))
    {
        adapter.Fill(tableNames);
    }

    var names = new string[tableNames.Rows.Count];
    for (var i = 0; i < tableNames.Rows.Count; i++)
    {
        names[i] = tableNames.Rows[i]["name"].ToString() ?? string.Empty;
    }
    return names;
}
