using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class CharactersPage : Page
{
    public CharactersPage(CharactersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
