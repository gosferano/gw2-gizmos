using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The price alerts, under the trading post.</summary>
public partial class PriceAlertsPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public PriceAlertsPage()
    {
        InitializeComponent();
    }

    public PriceAlertsPage(PriceAlertsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
