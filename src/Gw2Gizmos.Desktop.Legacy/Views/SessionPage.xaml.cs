using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class SessionPage : Page
{
    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
