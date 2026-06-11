using System.Windows.Controls;

namespace Gw2Gizmos.Herald;

public partial class NotificationsPage : Page
{
    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
