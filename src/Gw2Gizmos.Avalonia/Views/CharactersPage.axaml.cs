using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Views;

/// <summary>The selected account's characters, each opening its own page.</summary>
public partial class CharactersPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public CharactersPage()
    {
        InitializeComponent();
    }

    public CharactersPage(CharactersViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
