using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A small image that shows a GW2 currency's icon, resolved and cached by its URL through
/// <see cref="App.Icons"/>. Currencies aren't items, so they're keyed by URL rather than item id (the
/// item-id <see cref="ItemImage"/> can't be used). Loads asynchronously and guards against recycling.
/// </summary>
public sealed class CurrencyImage : Image
{
    public static readonly DependencyProperty IconUrlProperty = DependencyProperty.Register(
        nameof(IconUrl),
        typeof(string),
        typeof(CurrencyImage),
        new PropertyMetadata(null, OnIconUrlChanged)
    );

    public string? IconUrl
    {
        get => (string?)GetValue(IconUrlProperty);
        set => SetValue(IconUrlProperty, value);
    }

    private static async void OnIconUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var image = (CurrencyImage)d;
        var url = (string?)e.NewValue;
        image.Source = null;

        IconProvider? icons = App.Icons;
        if (icons is null || string.IsNullOrEmpty(url))
        {
            return;
        }

        ImageSource? source = await icons.GetIconByUrlAsync(url);

        // The row may have been recycled to a different currency while awaiting; only apply if still ours.
        if (image.IconUrl == url)
        {
            image.Source = source;
        }
    }
}
