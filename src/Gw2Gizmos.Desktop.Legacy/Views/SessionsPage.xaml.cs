using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class SessionsPage : Page
{
    public SessionsPage(SessionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
