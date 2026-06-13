using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class SharedInventoryPage : Page
{
    public SharedInventoryPage(SharedInventoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
