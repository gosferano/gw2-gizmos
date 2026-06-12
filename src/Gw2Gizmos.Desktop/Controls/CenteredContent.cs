using System;
using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A responsive content host: the child fills the full available width on narrow windows, but is
/// capped at <see cref="MaxContentWidth"/> and horizontally centered on wide ones (like CSS
/// <c>max-width</c> + <c>margin: 0 auto</c>). Plain WPF alignment can't express "stretch, then cap,
/// then center", hence this small layout control. Vertical layout is passed straight through, so it
/// composes with whatever the page does for height (scrolling lists, etc.).
/// </summary>
public sealed class CenteredContent : Decorator
{
    public static readonly DependencyProperty MaxContentWidthProperty = DependencyProperty.Register(
        nameof(MaxContentWidth),
        typeof(double),
        typeof(CenteredContent),
        new FrameworkPropertyMetadata(
            1100.0,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange
        )
    );

    /// <summary>The width the content is capped at on wide windows. Default 1100.</summary>
    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        UIElement? child = Child;
        if (child is null)
        {
            return default;
        }

        double cap = double.IsInfinity(constraint.Width) ? double.PositiveInfinity : Math.Min(constraint.Width, MaxContentWidth);
        child.Measure(new Size(cap, constraint.Height));

        // Occupy the full available width (so there's room to center within); flow height through.
        double width = double.IsInfinity(constraint.Width) ? child.DesiredSize.Width : constraint.Width;
        return new Size(width, child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        UIElement? child = Child;
        if (child is null)
        {
            return arrangeSize;
        }

        double width = Math.Min(arrangeSize.Width, MaxContentWidth);
        double x = Math.Max(0, (arrangeSize.Width - width) / 2);
        child.Arrange(new Rect(x, 0, width, arrangeSize.Height));
        return arrangeSize;
    }
}
