using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A small image that shows a GW2 item's icon, resolved and cached by item id through <see cref="App.Icons"/>.
/// Drop it next to an item name anywhere a row exposes an <c>ItemId</c> — the market grid, the craft tree,
/// etc. Loads asynchronously and guards against DataGrid/TreeView container recycling.
/// </summary>
public sealed class ItemImage : Image
{
    public static readonly DependencyProperty ItemIdProperty = DependencyProperty.Register(
        nameof(ItemId),
        typeof(int),
        typeof(ItemImage),
        new PropertyMetadata(0, OnItemIdChanged)
    );

    public int ItemId
    {
        get => (int)GetValue(ItemIdProperty);
        set => SetValue(ItemIdProperty, value);
    }

    private static async void OnItemIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var image = (ItemImage)d;
        var itemId = (int)e.NewValue;
        image.Source = null;

        IconProvider? icons = App.Icons;
        if (icons is null || itemId <= 0)
        {
            return;
        }

        ImageSource? source = await icons.GetIconAsync(itemId);

        // The container may have been recycled to a different item while we awaited; only apply if still ours.
        if (image.ItemId == itemId)
        {
            image.Source = source;
        }
    }
}
