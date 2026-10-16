using Avalonia;
using Avalonia.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A responsive content host: the child fills the full available width on narrow windows, but is capped at
/// <see cref="MaxContentWidth"/> and horizontally centered on wide ones (like CSS <c>max-width</c> +
/// <c>margin: 0 auto</c>). Plain alignment can't express "stretch, then cap, then center", hence this small layout
/// control. Vertical layout is passed straight through.
/// </summary>
public sealed class CenteredContent : Decorator
{
    public static readonly StyledProperty<double> MaxContentWidthProperty =
        AvaloniaProperty.Register<CenteredContent, double>(nameof(MaxContentWidth), 1100.0);

    static CenteredContent()
    {
        AffectsMeasure<CenteredContent>(MaxContentWidthProperty);
    }

    /// <summary>The width the content is capped at on wide windows. Default 1100.</summary>
    public double MaxContentWidth
    {
        get => GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        double cap = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : Math.Min(availableSize.Width, MaxContentWidth);
        child.Measure(new Size(cap, availableSize.Height));

        // Occupy the full available width (so there's room to center within); flow height through.
        double width = double.IsInfinity(availableSize.Width) ? child.DesiredSize.Width : availableSize.Width;
        return new Size(width, child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child)
        {
            return finalSize;
        }

        double width = Math.Min(finalSize.Width, MaxContentWidth);
        double x = Math.Max(0, (finalSize.Width - width) / 2);
        child.Arrange(new Rect(x, 0, width, finalSize.Height));
        return finalSize;
    }
}
