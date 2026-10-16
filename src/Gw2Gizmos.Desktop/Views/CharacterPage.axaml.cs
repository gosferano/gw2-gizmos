using Avalonia.Controls;
using Avalonia.Interactivity;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Views;

/// <summary>Characters › a character: links to its sub-sections (currently its inventory).</summary>
public partial class CharacterPage : UserControl
{
    private readonly INavigationService? _navigation;

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public CharacterPage()
    {
        InitializeComponent();
    }

    public CharacterPage(CharacterViewModel viewModel, INavigationService navigation)
        : this()
    {
        DataContext = viewModel;
        _navigation = navigation;
    }

    private void OnOpenInventory(object? sender, RoutedEventArgs e) =>
        _navigation?.NavigateTo(AppPage.CharacterInventory);
}
