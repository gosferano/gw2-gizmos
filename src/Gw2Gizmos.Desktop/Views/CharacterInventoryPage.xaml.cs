using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class CharacterInventoryPage : Page
{
    public CharacterInventoryPage(CharacterInventoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
