using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class SharedInventoryPage : Page
{
    public SharedInventoryPage(SharedInventoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
