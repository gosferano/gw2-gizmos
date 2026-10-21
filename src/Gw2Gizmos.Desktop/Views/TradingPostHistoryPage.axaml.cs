using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The account's completed trades, newest first, with what each sale made.</summary>
public partial class TradingPostHistoryPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public TradingPostHistoryPage()
    {
        InitializeComponent();
    }

    public TradingPostHistoryPage(TradingPostHistoryViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
