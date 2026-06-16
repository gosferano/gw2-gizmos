using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnOpenAdvanced(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(AdvancedSettingsPage));

    private void OnOpenStoredData(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(StoredDataPage));
}
