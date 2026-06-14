using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class AccountPage : Page
{
    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnOpenWallet(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(WalletPage));

    private void OnOpenMaterials(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(MaterialStoragePage));

    private void OnOpenBank(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(BankPage));

    private void OnOpenSharedInventory(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(SharedInventoryPage));
}
