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

    /// <summary>Rounds the icon's corners (0 = square). Clips the image to a rounded rectangle.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius),
        typeof(double),
        typeof(ItemImage),
        new PropertyMetadata(0.0, (d, _) => ((ItemImage)d).UpdateClip())
    );

    public ItemImage()
    {
        // The clip depends on the rendered size, which is only known after layout (and changes as the
        // Uniform-stretched icon resizes with its column).
        SizeChanged += (_, _) => UpdateClip();
    }

    public int ItemId
    {
        get => (int)GetValue(ItemIdProperty);
        set => SetValue(ItemIdProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private void UpdateClip()
    {
        if (CornerRadius <= 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            Clip = null;
            return;
        }

        Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), CornerRadius, CornerRadius);
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
