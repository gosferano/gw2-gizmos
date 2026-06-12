using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class MarketPage : Page
{
    public MarketPage(MarketViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
