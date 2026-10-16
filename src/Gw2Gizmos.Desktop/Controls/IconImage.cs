using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Gw2Gizmos.Desktop.Navigation;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A small image that shows a GW2 icon by its render URL (a currency, a profession, a specialization), resolved and
/// cached through <see cref="AppServices.Icons"/>. For items, use the item-id <see cref="ItemImage"/>. Loads
/// asynchronously and guards against recycling.
/// </summary>
public sealed class IconImage : Image
{
    public static readonly StyledProperty<string?> IconUrlProperty =
        AvaloniaProperty.Register<IconImage, string?>(nameof(IconUrl));

    protected override Type StyleKeyOverride => typeof(Image);

    public string? IconUrl
    {
        get => GetValue(IconUrlProperty);
        set => SetValue(IconUrlProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconUrlProperty)
        {
            _ = LoadAsync(IconUrl);
        }
    }

    private async Task LoadAsync(string? url)
    {
        Source = null;

        IconProvider<Bitmap>? icons = AppServices.Icons;
        if (icons is null || string.IsNullOrEmpty(url))
        {
            return;
        }

        Bitmap? bitmap = await icons.GetIconByUrlAsync(url);

        // The row may have been recycled to a different icon while awaiting; only apply if still ours.
        if (IconUrl == url)
        {
            Source = bitmap;
        }
    }
}
