using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Realized profit from the account's own trades, by period and by item.</summary>
public partial class TradingPostProfitPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public TradingPostProfitPage()
    {
        InitializeComponent();
    }

    public TradingPostProfitPage(TradingPostProfitViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
