using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class WalletPage : Page
{
    public WalletPage(WalletViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
