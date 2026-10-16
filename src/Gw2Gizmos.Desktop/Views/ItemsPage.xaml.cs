using System.Windows.Controls;
using Gw2Gizmos.Desktop.Charts;

namespace Gw2Gizmos.Desktop;

public partial class ItemsPage : Page
{
    public ItemsPage(ItemsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        // The price charts are drawn with LiveCharts from the shared, chart-agnostic history view-model.
        HistoryChart.DataContext = new LiveChartsPriceHistory(viewModel.History);
    }
}
