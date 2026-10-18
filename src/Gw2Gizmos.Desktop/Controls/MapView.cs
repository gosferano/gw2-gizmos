using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Navigation;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A pannable, zoomable map of a continent drawn from the official tiles (through <see cref="AppServices.Tiles"/>).
/// Drag to pan, scroll to zoom around the pointer, double-click to zoom in. <see cref="CenterX"/>,
/// <see cref="CenterY"/> and <see cref="Zoom"/> are the view, in continent units and tile zoom levels; the zoom is
/// fractional, drawn from the next tile level up scaled down. Tiles still loading are stood in for by a lower level's
/// tile already in memory, scaled up.
/// </summary>
public sealed class MapView : Control
{
    public static readonly StyledProperty<MapContinent?> ContinentProperty =
        AvaloniaProperty.Register<MapView, MapContinent?>(nameof(Continent));

    public static readonly StyledProperty<double> CenterXProperty = AvaloniaProperty.Register<MapView, double>(
        nameof(CenterX),
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
    );

    public static readonly StyledProperty<double> CenterYProperty = AvaloniaProperty.Register<MapView, double>(
        nameof(CenterY),
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
    );

    public static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<MapView, double>(
        nameof(Zoom),
        3,
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
    );

    public static readonly StyledProperty<double> MinZoomProperty = AvaloniaProperty.Register<MapView, double>(
        nameof(MinZoom),
        1
    );

    public static readonly StyledProperty<double> MaxZoomProperty = AvaloniaProperty.Register<MapView, double>(
        nameof(MaxZoom),
        8
    );

    /// <summary>How much one notch of the mouse wheel zooms.</summary>
    private const double WheelZoomStep = 0.5;

    /// <summary>The deep sea the tiles fade into, behind tiles that haven't loaded and beyond the drawn area.</summary>
    private static readonly IBrush Sea = new SolidColorBrush(Color.FromRgb(0x0B, 0x1E, 0x2E));

    private Point? _dragFrom;

    /// <summary>The tiles the latest render drew (read from download threads, so swapped whole).</summary>
    private volatile VisibleTiles? _visible;

    static MapView()
    {
        AffectsRender<MapView>(ContinentProperty, CenterXProperty, CenterYProperty, ZoomProperty);
        FocusableProperty.OverrideDefaultValue<MapView>(true);
    }

    public MapView()
    {
        ClipToBounds = true;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        DoubleTapped += (_, e) => ZoomAround(e.GetPosition(this), 1);
    }

    public MapContinent? Continent
    {
        get => GetValue(ContinentProperty);
        set => SetValue(ContinentProperty, value);
    }

    /// <summary>The continent point shown at the middle of the control.</summary>
    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    public double CenterY
    {
        get => GetValue(CenterYProperty);
        set => SetValue(CenterYProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    /// <summary>Screen pixels per continent unit at the current zoom.</summary>
    private double Scale => Math.Pow(2, Zoom - MapContinent.NativeZoom);

    public override void Render(DrawingContext context)
    {
        Rect bounds = new(Bounds.Size);
        context.FillRectangle(Sea, bounds);

        MapContinent? continent = Continent;
        MapTileCache<Bitmap>? tiles = AppServices.Tiles;
        if (continent is null || tiles is null)
        {
            return;
        }

        int tileZoom = Math.Clamp((int)Math.Ceiling(Zoom - 0.001), 0, continent.MaxTileZoom);
        double unitsPerTile = MapContinent.UnitsPerTile(tileZoom);
        double scale = Scale;

        // The continent area on screen, clipped to the continent itself.
        double left = CenterX - bounds.Width / 2 / scale;
        double top = CenterY - bounds.Height / 2 / scale;
        int firstColumn = Math.Max(0, (int)Math.Floor(left / unitsPerTile));
        int firstRow = Math.Max(0, (int)Math.Floor(top / unitsPerTile));
        int lastColumn = Math.Min(
            (int)Math.Ceiling(continent.Width / unitsPerTile) - 1,
            (int)Math.Floor((left + bounds.Width / scale) / unitsPerTile)
        );
        int lastRow = Math.Min(
            (int)Math.Ceiling(continent.Height / unitsPerTile) - 1,
            (int)Math.Floor((top + bounds.Height / scale) / unitsPerTile)
        );

        _visible = new VisibleTiles(tileZoom, firstColumn, firstRow, lastColumn, lastRow);

        for (int row = firstRow; row <= lastRow; row++)
        {
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                // Edges rounded to whole pixels from each tile's own corner, so neighbouring tiles meet without seams.
                double x0 = Math.Floor((column * unitsPerTile - left) * scale);
                double y0 = Math.Floor((row * unitsPerTile - top) * scale);
                double x1 = Math.Floor(((column + 1) * unitsPerTile - left) * scale);
                double y1 = Math.Floor(((row + 1) * unitsPerTile - top) * scale);
                Rect destination = new(x0, y0, x1 - x0, y1 - y0);

                MapTileKey key = new(continent.Id, continent.Floor, tileZoom, column, row);
                if (tiles.TryGet(key) is { } tile)
                {
                    context.DrawImage(tile, new Rect(tile.Size), destination);
                    continue;
                }

                if (tiles.IsMissing(key))
                {
                    continue;
                }

                DrawStandIn(context, tiles, continent, key, destination);
                RequestTile(tiles, continent, key);
            }
        }
    }

    /// <summary>Fills a tile still loading with the part of the nearest lower-zoom tile that's already in memory.</summary>
    private static void DrawStandIn(
        DrawingContext context,
        MapTileCache<Bitmap> tiles,
        MapContinent continent,
        MapTileKey key,
        Rect destination
    )
    {
        for (int levelsUp = 1; levelsUp <= key.Zoom; levelsUp++)
        {
            int factor = 1 << levelsUp;
            MapTileKey parentKey = key with { Zoom = key.Zoom - levelsUp, X = key.X / factor, Y = key.Y / factor };
            if (tiles.TryGet(parentKey) is not { } parent)
            {
                continue;
            }

            double size = parent.Size.Width / factor;
            Rect source = new((key.X % factor) * size, (key.Y % factor) * size, size, size);
            context.DrawImage(parent, source, destination);
            return;
        }
    }

    /// <summary>
    /// Loads a tile and redraws once it's in. A tile the map has panned or zoomed away from by the time a download
    /// slot frees up is skipped.
    /// </summary>
    private void RequestTile(MapTileCache<Bitmap> tiles, MapContinent continent, MapTileKey key)
    {
        string url = continent.TileUrl(key.Zoom, key.X, key.Y);
        _ = tiles
            .LoadAsync(key, url, () => _visible?.Contains(key) == true)
            .ContinueWith(
                task =>
                {
                    if (task.Result is not null)
                    {
                        Dispatcher.UIThread.Post(InvalidateVisual);
                    }
                },
                TaskScheduler.Default
            );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragFrom = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragFrom is not { } from)
        {
            return;
        }

        Point to = e.GetPosition(this);
        double scale = Scale;
        MoveCenterTo(CenterX - (to.X - from.X) / scale, CenterY - (to.Y - from.Y) / scale);
        _dragFrom = to;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragFrom = null;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragFrom = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomAround(e.GetPosition(this), e.Delta.Y * WheelZoomStep);
        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ZoomProperty)
        {
            double clamped = Math.Clamp(Zoom, MinZoom, MaxZoom);
            if (clamped != Zoom)
            {
                Zoom = clamped;
            }
        }
    }

    /// <summary>Zooms by <paramref name="delta"/> levels, keeping the continent point under <paramref name="anchor"/> still.</summary>
    private void ZoomAround(Point anchor, double delta)
    {
        double before = Scale;
        double zoom = Math.Clamp(Zoom + delta, MinZoom, MaxZoom);
        if (zoom == Zoom)
        {
            return;
        }

        // The continent point under the anchor, before and after: shift the centre so it stays put.
        double offsetX = anchor.X - Bounds.Width / 2;
        double offsetY = anchor.Y - Bounds.Height / 2;
        double anchorX = CenterX + offsetX / before;
        double anchorY = CenterY + offsetY / before;
        Zoom = zoom;
        double after = Scale;
        MoveCenterTo(anchorX - offsetX / after, anchorY - offsetY / after);
    }

    /// <summary>Moves the view, keeping its centre within the area of the continent worth showing.</summary>
    private void MoveCenterTo(double x, double y)
    {
        if (Continent is { } continent)
        {
            x = Math.Clamp(x, continent.ViewMinX, continent.ViewMaxX);
            y = Math.Clamp(y, continent.ViewMinY, continent.ViewMaxY);
        }

        CenterX = x;
        CenterY = y;
    }

    /// <summary>A zoom level's range of tile columns and rows.</summary>
    private sealed record VisibleTiles(int Zoom, int FirstColumn, int FirstRow, int LastColumn, int LastRow)
    {
        public bool Contains(MapTileKey key) =>
            key.Zoom == Zoom && key.X >= FirstColumn && key.X <= LastColumn && key.Y >= FirstRow && key.Y <= LastRow;
    }
}
