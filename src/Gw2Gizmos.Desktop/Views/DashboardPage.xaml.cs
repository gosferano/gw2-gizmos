using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class DashboardPage : Page
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
