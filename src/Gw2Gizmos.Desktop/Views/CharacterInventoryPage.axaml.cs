using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Characters › a character › Inventory: the character's bag slots.</summary>
public partial class CharacterInventoryPage : UserControl
{
    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public CharacterInventoryPage()
    {
        InitializeComponent();
    }

    public CharacterInventoryPage(CharacterInventoryViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
