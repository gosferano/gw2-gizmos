using System.Windows.Controls;

namespace Gw2Gizmos.Desktop;

public partial class ProfitableRecipesPage : Page
{
    public ProfitableRecipesPage(ProfitableRecipesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
