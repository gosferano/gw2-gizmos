using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The account's open trading post orders, beaten ones first, with their age and which are stale.</summary>
public partial class TradingPostOrdersPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public TradingPostOrdersPage()
    {
        InitializeComponent();
    }

    public TradingPostOrdersPage(TradingPostOrdersViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
