using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The selected account's open trading-post orders and its log of completed trades.</summary>
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
