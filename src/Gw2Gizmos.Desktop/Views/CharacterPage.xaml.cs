using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class CharacterPage : Page
{
    public CharacterPage(CharacterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnOpenInventory(object sender, RoutedEventArgs e) => App.NavigateTo(typeof(CharacterInventoryPage));
}
