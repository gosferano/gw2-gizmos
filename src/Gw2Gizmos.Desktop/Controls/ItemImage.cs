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
/// Loads asynchronously and guards against container recycling. Outlined in the item's rarity colour, like in game
/// (none for Basic items). Hovering it shows the item's prices (<see cref="ItemTooltip"/>).
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

    // The item's rarity outline (e.g. "RarityExoticBrush"), or null for none.
    private string? _rarityBrushKey;

    // The outline's on-screen thickness, whatever the icon's size.
    private const double OutlineThickness = 1.5;

    // The ring width (in icon pixels) the shown icon was processed with, to redo it only when the size changes it.
    private int _outlinePixels;

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
            ItemTooltip.SetItemId(this, ItemId);
            _ = LoadAsync(ItemId);
            _ = LoadRarityAsync(ItemId);
        }
        else if (change.Property == DesaturateProperty)
        {
            ApplySource();
        }
        else if (change.Property == CornerRadiusProperty || change.Property == BoundsProperty)
        {
            // The clip depends on the rendered size, which changes as the Uniform-stretched icon resizes.
            UpdateClip();

            // So does the outline's ring, to keep it the same thickness on screen.
            if (change.Property == BoundsProperty && _rarityBrushKey is not null && OutlinePixels() != _outlinePixels)
            {
                ApplySource();
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnThemeChanged;

        // The icon may have loaded before the control was on a page, when its outline's colour couldn't be found yet.
        ApplySource();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ActualThemeVariantChanged -= OnThemeChanged;
        base.OnDetachedFromVisualTree(e);
    }

    // The outline's shade follows the theme.
    private void OnThemeChanged(object? sender, EventArgs e) => ApplySource();

    private async Task LoadRarityAsync(int itemId)
    {
        _rarityBrushKey = null;
        if (AppServices.Rarities is not { } rarities || itemId <= 0)
        {
            return;
        }

        await rarities.Ready;
        if (ItemId == itemId)
        {
            string? rarity = rarities.Of(itemId);
            _rarityBrushKey = rarity is null or "Basic" ? null : $"Rarity{rarity}Brush";
            ApplySource();
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

    // Shows the loaded icon, desaturated when Desaturate is set, outlined in its rarity colour.
    private void ApplySource()
    {
        if (_loaded is null)
        {
            Source = null;
            return;
        }

        Color? outline = _rarityBrushKey is not null && FindColor(_rarityBrushKey) is { } color ? color : null;
        _outlinePixels = outline is not null ? OutlinePixels() : 0;
        Source = Desaturate || outline is not null
            ? Process(_loaded, Desaturate, outline, _outlinePixels, CornerRadiusPixels())
            : _loaded;
    }

    /// <summary>A theme colour by resource key: from the page if on one, otherwise the app's resources.</summary>
    private Color? FindColor(string key)
    {
        object? found = null;
        bool ok = this.TryFindResource(key, ActualThemeVariant, out found)
            || Application.Current?.TryFindResource(key, ActualThemeVariant, out found) == true;
        return ok && found is ISolidColorBrush brush ? brush.Color : null;
    }

    /// <summary>The <see cref="CornerRadius"/> in icon pixels, so the ring follows the rounded clip's corners.</summary>
    private double CornerRadiusPixels()
    {
        int iconWidth = _loaded?.PixelSize.Width ?? 64;
        double shown = Math.Min(Bounds.Width, Bounds.Height);
        return CornerRadius <= 0 ? 0 : shown > 0 ? CornerRadius * iconWidth / shown : CornerRadius;
    }

    /// <summary>
    /// The outline's ring width in icon pixels for a constant <see cref="OutlineThickness"/> on screen: the icon is
    /// scaled to the control, so the ring scales inversely (before layout, a sixteenth of the icon).
    /// </summary>
    private int OutlinePixels()
    {
        int iconWidth = _loaded?.PixelSize.Width ?? 64;
        double shown = Math.Min(Bounds.Width, Bounds.Height);
        int pixels = shown > 0 ? (int)Math.Round(OutlineThickness * iconWidth / shown) : iconWidth / 16;
        return Math.Clamp(pixels, 1, Math.Max(1, iconWidth / 4));
    }

    /// <summary>
    /// A processed copy of the icon: greyscale (Rec. 601 luminance per pixel, alpha preserved, so transparent regions
    /// stay so) when <paramref name="grayscale"/>, and with a ring of <paramref name="outline"/> colour,
    /// <paramref name="ring"/> icon pixels wide, around its edge — following rounded corners of
    /// <paramref name="radius"/> icon pixels, so the clip that rounds the icon doesn't cut the ring.
    /// </summary>
    private static unsafe Bitmap Process(Bitmap source, bool grayscale, Color? outline, int ring, double radius)
    {
        var result = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using ILockedFramebuffer buffer = result.Lock();
        source.CopyPixels(buffer);

        int width = buffer.Size.Width;
        int height = buffer.Size.Height;
        for (int y = 0; y < height; y++)
        {
            byte* row = (byte*)buffer.Address + (y * buffer.RowBytes);
            for (int x = 0; x < width; x++)
            {
                byte* pixel = row + (x * 4);
                if (outline is { } color && InRing(x + 0.5, y + 0.5, width, height, ring, radius))
                {
                    // BGRA, opaque (so premultiplied equals straight).
                    pixel[0] = color.B;
                    pixel[1] = color.G;
                    pixel[2] = color.R;
                    pixel[3] = 255;
                }
                else if (grayscale)
                {
                    // Luminance is linear, so it stays valid on premultiplied channels.
                    byte gray = (byte)(((pixel[0] * 114) + (pixel[1] * 587) + (pixel[2] * 299)) / 1000);
                    pixel[0] = pixel[1] = pixel[2] = gray;
                }
            }
        }

        return result;
    }

    /// <summary>Whether a point is inside the icon's rounded edge but outside the same shape inset by the ring.</summary>
    private static bool InRing(double x, double y, int width, int height, int ring, double radius) =>
        InRoundedRect(x, y, 0, 0, width, height, radius)
        && !InRoundedRect(x, y, ring, ring, width - ring, height - ring, Math.Max(0, radius - ring));

    private static bool InRoundedRect(double x, double y, double left, double top, double right, double bottom, double radius)
    {
        if (x < left || y < top || x > right || y > bottom)
        {
            return false;
        }

        // Away from the corners it's a plain rectangle; at a corner, within the radius of the corner's centre.
        double cx = Math.Clamp(x, left + radius, right - radius);
        double cy = Math.Clamp(y, top + radius, bottom - radius);
        return ((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= radius * radius;
    }
}
