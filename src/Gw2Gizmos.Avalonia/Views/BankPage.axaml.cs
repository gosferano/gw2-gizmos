using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>Account › Bank: the account bank's slots.</summary>
public partial class BankPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public BankPage()
    {
        InitializeComponent();
    }

    public BankPage(BankViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
