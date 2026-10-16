using Avalonia.Controls;
using Avalonia.Interactivity;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>Settings: launch at login, the worker's feature toggles, and links to Advanced and Stored data.</summary>
public partial class SettingsPage : UserControl
{
    private readonly INavigationService? _navigation;

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsPage(SettingsViewModel viewModel, INavigationService navigation)
        : this()
    {
        DataContext = viewModel;
        _navigation = navigation;
    }

    private void OnOpenAdvanced(object? sender, RoutedEventArgs e) => _navigation?.NavigateTo(AppPage.AdvancedSettings);

    private void OnOpenStoredData(object? sender, RoutedEventArgs e) => _navigation?.NavigateTo(AppPage.StoredData);
}
