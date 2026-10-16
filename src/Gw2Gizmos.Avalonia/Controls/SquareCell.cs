using Avalonia;
using Avalonia.Controls;

namespace Gw2Gizmos.Avalonia.Controls;

/// <summary>
/// Lays its child out in a square as wide as the space it's given, so grid cells (bank / inventory / material slots in
/// a 10-column UniformGrid) keep equal spacing on both axes. The WPF app bound each cell's Height to its ActualWidth;
/// doing it in measure avoids that layout feedback loop.
/// </summary>
public sealed class SquareCell : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double side = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        Child?.Measure(new Size(side, side));
        if (side == 0 && Child is not null)
        {
            side = Math.Max(Child.DesiredSize.Width, Child.DesiredSize.Height);
        }

        return new Size(side, side);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
