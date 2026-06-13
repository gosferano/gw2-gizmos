using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class BankPage : Page
{
    public BankPage(BankViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
