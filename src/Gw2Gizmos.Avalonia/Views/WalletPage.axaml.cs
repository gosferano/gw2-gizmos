using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>Account › Wallet: currencies and coin.</summary>
public partial class WalletPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public WalletPage()
    {
        InitializeComponent();
    }

    public WalletPage(WalletViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
