using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Gw2Gizmos.Desktop.Controls;
using Gw2Gizmos.Desktop.Views;
using Xunit.Abstractions;

namespace Gw2Gizmos.Desktop.Tests;

/// <summary>The Items page's grid, run headless with real fonts: its coin columns fit the widest amounts.</summary>
public sealed class ItemsPageTests(ITestOutputHelper output)
{
    // The widest amounts a coin column shows: 9,999g 99s 99c, and with a minus sign in the profit columns.
    private const long Widest = 99_999_999;

    // The cell's left margin around the coins (8) and room on the right before the next column.
    private const double CellPadding = 8 + 8;

    [Theory]
    [InlineData("Buy", false)]
    [InlineData("Sell", false)]
    [InlineData("Craft cost", false)]
    [InlineData("Buy profit", true)]
    [InlineData("Sell profit", true)]
    public Task A_coin_column_fits_the_widest_amount(string header, bool signed) =>
        HeadlessSession.Run(() =>
        {
            var page = new ItemsPage();
            var window = new Window { Width = 1600, Height = 900, Content = page };
            window.Show();

            DataGridColumn column = page.GetVisualDescendants().OfType<DataGrid>().First()
                .Columns.Single(c => Equals(c.Header, header));
            double needed = CoinWidth(window, signed ? -Widest : Widest) + CellPadding;
            output.WriteLine($"{header}: column {column.Width.Value:0}px, needs {needed:0.#}px");

            Assert.True(column.Width.Value >= needed, $"{header} is {column.Width.Value}px, needs {needed:0.#}px");
            window.Close();
            return Task.CompletedTask;
        });

    private static double CoinWidth(Window window, long copper)
    {
        var coins = new CoinDisplay { Copper = copper, Signed = copper < 0 };
        window.Content = coins;
        coins.Measure(Size.Infinity);
        return coins.DesiredSize.Width;
    }
}
