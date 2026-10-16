using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class DashboardPage : Page
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
