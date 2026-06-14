using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class ItemsPage : Page
{
    public ItemsPage(ItemsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
