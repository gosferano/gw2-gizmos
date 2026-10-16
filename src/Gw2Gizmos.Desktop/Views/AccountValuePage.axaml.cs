using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>The selected account's estimated value over time.</summary>
public partial class AccountValuePage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public AccountValuePage()
    {
        InitializeComponent();
    }

    public AccountValuePage(AccountValueViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
