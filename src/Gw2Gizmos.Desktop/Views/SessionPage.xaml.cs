using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class SessionPage : Page
{
    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
