using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A thin progress bar with a tick where each tier ends, for an achievement's progress toward its final tier.
/// </summary>
public sealed class TierBar : Control
{
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<TierBar, double>(
        nameof(Value)
    );

    public static readonly StyledProperty<IReadOnlyList<double>?> MarksProperty = AvaloniaProperty.Register<
        TierBar,
        IReadOnlyList<double>?
    >(nameof(Marks));

    public static readonly StyledProperty<IBrush?> TrackProperty = AvaloniaProperty.Register<TierBar, IBrush?>(
        nameof(Track)
    );

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<TierBar, IBrush?>(
        nameof(Fill)
    );

    public static readonly StyledProperty<IBrush?> MarkProperty = AvaloniaProperty.Register<TierBar, IBrush?>(
        nameof(Mark)
    );

    static TierBar()
    {
        AffectsRender<TierBar>(ValueProperty, MarksProperty, TrackProperty, FillProperty, MarkProperty);
    }

    /// <summary>How far along the bar is filled, 0 to 1.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Where each tier ends along the bar, 0 to 1.</summary>
    public IReadOnlyList<double>? Marks
    {
        get => GetValue(MarksProperty);
        set => SetValue(MarksProperty, value);
    }

    public IBrush? Track
    {
        get => GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Mark
    {
        get => GetValue(MarkProperty);
        set => SetValue(MarkProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        Rect bounds = new(Bounds.Size);
        double radius = bounds.Height / 2;
        context.DrawRectangle(Track, null, bounds, radius, radius);

        double filled = bounds.Width * Math.Clamp(Value, 0, 1);
        if (filled > 0)
        {
            context.DrawRectangle(Fill, null, new Rect(0, 0, filled, bounds.Height), radius, radius);
        }

        if (Marks is { Count: > 0 } marks && Mark is { } mark)
        {
            var pen = new Pen(mark, 2);
            foreach (double at in marks)
            {
                double x = Math.Round(bounds.Width * Math.Clamp(at, 0, 1));
                context.DrawLine(pen, new Point(x, 0), new Point(x, bounds.Height));
            }
        }
    }
}
