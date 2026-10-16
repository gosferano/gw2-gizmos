using Avalonia.Controls;
using Avalonia.Interactivity;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The selected account's hub: its summary and links to wallet, materials, bank and shared inventory.</summary>
public partial class AccountPage : UserControl
{
    private readonly INavigationService? _navigation;

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public AccountPage()
    {
        InitializeComponent();
    }

    public AccountPage(AccountViewModel viewModel, INavigationService navigation)
        : this()
    {
        DataContext = viewModel;
        _navigation = navigation;
    }

    private void OnOpenValue(object? sender, RoutedEventArgs e) => _navigation?.NavigateTo(AppPage.AccountValue);

    private void OnOpenWallet(object? sender, RoutedEventArgs e) => _navigation?.NavigateTo(AppPage.Wallet);

    private void OnOpenMaterials(object? sender, RoutedEventArgs e) => _navigation?.NavigateTo(AppPage.MaterialStorage);

    private void OnOpenBank(object? sender, RoutedEventArgs e) => _navigation?.NavigateTo(AppPage.Bank);

    private void OnOpenSharedInventory(object? sender, RoutedEventArgs e) =>
        _navigation?.NavigateTo(AppPage.SharedInventory);
}
