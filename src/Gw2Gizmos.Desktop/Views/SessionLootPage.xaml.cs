using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class SessionLootPage : Page
{
    public SessionLootPage(SessionLootViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
