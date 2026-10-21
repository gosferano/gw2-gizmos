using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The trading post hub: orders, history, profit and price alerts, each summarised.</summary>
public partial class TradingPostPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public TradingPostPage()
    {
        InitializeComponent();
    }

    public TradingPostPage(TradingPostViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
