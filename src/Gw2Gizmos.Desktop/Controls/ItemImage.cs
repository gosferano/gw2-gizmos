using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    /// <summary>When true, the icon is rendered greyscale (e.g. a material the account doesn't hold). The
    /// desaturation is computed from the loaded icon in memory — no second image is fetched or stored.</summary>
    public static readonly DependencyProperty DesaturateProperty = DependencyProperty.Register(
        nameof(Desaturate),
        typeof(bool),
        typeof(ItemImage),
        new PropertyMetadata(false, (d, _) => ((ItemImage)d).ApplySource())
    );

    // The icon as loaded (full colour); kept so toggling Desaturate re-renders without re-fetching.
    private ImageSource? _loaded;

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

    public bool Desaturate
    {
        get => (bool)GetValue(DesaturateProperty);
        set => SetValue(DesaturateProperty, value);
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
        image._loaded = null;
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
            image._loaded = source;
            image.ApplySource();
        }
    }

    // Shows the loaded icon, desaturated when Desaturate is set. Greyscale is derived from the in-memory
    // bitmap (RGB → luminance, alpha preserved), so transparent regions stay transparent and no extra image
    // is fetched or cached.
    private void ApplySource() =>
        Source = Desaturate && _loaded is BitmapSource colour ? ToGrayscale(colour) : _loaded;

    private static BitmapSource ToGrayscale(BitmapSource source)
    {
        var bgra = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = bgra.PixelWidth;
        int height = bgra.PixelHeight;
        int stride = width * 4;
        var pixels = new byte[height * stride];
        bgra.CopyPixels(pixels, stride, 0);

        for (var i = 0; i < pixels.Length; i += 4)
        {
            // Rec. 601 luminance; leave the alpha byte (i + 3) untouched.
            byte gray = (byte)((pixels[i] * 114 + pixels[i + 1] * 587 + pixels[i + 2] * 299) / 1000);
            pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
        }

        var result = BitmapSource.Create(
            width, height, bgra.DpiX, bgra.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }
}
