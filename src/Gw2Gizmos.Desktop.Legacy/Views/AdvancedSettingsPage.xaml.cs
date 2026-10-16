using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Legacy;

public partial class AdvancedSettingsPage : Page
{
    public AdvancedSettingsPage(AdvancedSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
