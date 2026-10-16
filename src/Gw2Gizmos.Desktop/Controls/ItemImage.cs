using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Gw2Gizmos.Desktop.Navigation;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A small image that shows a GW2 item's icon, resolved and cached by item id through <see cref="AppServices.Icons"/>.
/// Drop it next to an item name anywhere a row exposes an <c>ItemId</c> — the market grid, the craft tree, etc.
/// Loads asynchronously and guards against container recycling.
/// </summary>
public sealed class ItemImage : Image
{
    public static readonly StyledProperty<int> ItemIdProperty =
        AvaloniaProperty.Register<ItemImage, int>(nameof(ItemId));

    /// <summary>Rounds the icon's corners (0 = square) by clipping it to a rounded rectangle.</summary>
    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<ItemImage, double>(nameof(CornerRadius));

    /// <summary>When true, the icon is rendered greyscale (e.g. a material the account doesn't hold), computed from the
    /// loaded icon in memory — no second image is fetched or stored.</summary>
    public static readonly StyledProperty<bool> DesaturateProperty =
        AvaloniaProperty.Register<ItemImage, bool>(nameof(Desaturate));

    // The icon as loaded (full colour); kept so toggling Desaturate re-renders without re-fetching.
    private Bitmap? _loaded;

    protected override Type StyleKeyOverride => typeof(Image);

    public int ItemId
    {
        get => GetValue(ItemIdProperty);
        set => SetValue(ItemIdProperty, value);
    }

    public double CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public bool Desaturate
    {
        get => GetValue(DesaturateProperty);
        set => SetValue(DesaturateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemIdProperty)
        {
            _ = LoadAsync(ItemId);
        }
        else if (change.Property == DesaturateProperty)
        {
            ApplySource();
        }
        else if (change.Property == CornerRadiusProperty || change.Property == BoundsProperty)
        {
            // The clip depends on the rendered size, which changes as the Uniform-stretched icon resizes.
            UpdateClip();
        }
    }

    private void UpdateClip() =>
        Clip = CornerRadius > 0 && Bounds.Width > 0 && Bounds.Height > 0
            ? new RectangleGeometry(new Rect(Bounds.Size), CornerRadius, CornerRadius)
            : null;

    private async Task LoadAsync(int itemId)
    {
        _loaded = null;
        Source = null;

        IconProvider<Bitmap>? icons = AppServices.Icons;
        if (icons is null || itemId <= 0)
        {
            return;
        }

        Bitmap? bitmap = await icons.GetIconAsync(itemId);

        // The container may have been recycled to a different item while we awaited; only apply if still ours.
        if (ItemId == itemId)
        {
            _loaded = bitmap;
            ApplySource();
        }
    }

    // Shows the loaded icon, desaturated when Desaturate is set.
    private void ApplySource() => Source = Desaturate && _loaded is not null ? ToGrayscale(_loaded) : _loaded;

    /// <summary>A greyscale copy: Rec. 601 luminance per pixel, alpha preserved (so transparent regions stay so).</summary>
    private static unsafe Bitmap ToGrayscale(Bitmap source)
    {
        var result = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using ILockedFramebuffer buffer = result.Lock();
        source.CopyPixels(buffer);

        for (int y = 0; y < buffer.Size.Height; y++)
        {
            byte* row = (byte*)buffer.Address + (y * buffer.RowBytes);
            for (int x = 0; x < buffer.Size.Width; x++)
            {
                byte* pixel = row + (x * 4);
                // BGRA; luminance is linear, so it stays valid on premultiplied channels.
                byte gray = (byte)(((pixel[0] * 114) + (pixel[1] * 587) + (pixel[2] * 299)) / 1000);
                pixel[0] = pixel[1] = pixel[2] = gray;
            }
        }

        return result;
    }
}
