using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class SessionLootPage : Page
{
    public SessionLootPage(SessionLootViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
