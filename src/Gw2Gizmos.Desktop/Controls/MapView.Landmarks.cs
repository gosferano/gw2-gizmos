using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Navigation;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// The labels and markers drawn over the map, each kind from the zoom where the game's own map shows it: region names
/// when zoomed out, then map names with their levels, then waypoints, then the other markers, and sector names when
/// zoomed in. Hovering a marker shows its name; clicking one with a chat link runs <see cref="CopyChatLinkCommand"/>
/// with it.
/// </summary>
public sealed partial class MapView
{
    public static readonly StyledProperty<MapLandmarks?> LandmarksProperty = AvaloniaProperty.Register<
        MapView,
        MapLandmarks?
    >(nameof(Landmarks));

    public static readonly StyledProperty<MapPlayer?> PlayerProperty = AvaloniaProperty.Register<MapView, MapPlayer?>(
        nameof(Player)
    );

    public static readonly StyledProperty<bool> IsFollowingProperty = AvaloniaProperty.Register<MapView, bool>(
        nameof(IsFollowing),
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
    );

    public static readonly StyledProperty<ICommand?> CopyChatLinkCommandProperty = AvaloniaProperty.Register<
        MapView,
        ICommand?
    >(nameof(CopyChatLinkCommand));

    /// <summary>How far the pointer may move between press and release for it to still count as a click.</summary>
    private const double ClickTolerance = 4;

    private const double RegionLabelsBelowZoom = 3;
    private const double MapLabelsFromZoom = 3;
    private const double MapLabelsBelowZoom = 6;
    private const double SectorLabelsFromZoom = 6;
    private const double WaypointsFromZoom = 4;
    private const double MarkersFromZoom = 5;

    // The game's own map icons, from /v2/files.
    private const string WaypointIcon =
        "https://render.guildwars2.com/file/32633AF8ADEA696A1EF56D3AE32D617B10D3AC57/157353.png";
    private const string LandmarkIcon =
        "https://render.guildwars2.com/file/25B230711176AB5728E86F5FC5F0BFAE48B32F6E/97461.png";
    private const string VistaIcon =
        "https://render.guildwars2.com/file/A2C16AF497BA3A0903A0499FFBAF531477566F10/358415.png";
    private const string HeartIcon =
        "https://render.guildwars2.com/file/09ACBA53B7412CC3C76E7FEF39929843C20CB0E4/102440.png";
    private const string HeroPointIcon =
        "https://render.guildwars2.com/file/B4EC6BB3FDBC42557C3CAE0CAA9E57EBF9E462E3/156626.png";

    private static readonly IBrush RegionBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xE4, 0xB8));
    private static readonly IBrush MapBrush = Brushes.White;
    private static readonly IBrush DetailBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8));
    private static readonly IBrush SectorBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
    private static readonly IPen TextOutline = new Pen(
        new SolidColorBrush(Color.FromArgb(0xD0, 0x00, 0x00, 0x00)),
        3,
        lineJoin: PenLineJoin.Round
    );
    private static readonly IBrush TooltipBackground = new SolidColorBrush(Color.FromArgb(0xEE, 0x1E, 0x1E, 0x1E));
    private static readonly IBrush TooltipHint = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
    private static readonly IBrush PendingIcon = new SolidColorBrush(Color.FromArgb(0xC0, 0xF2, 0xE4, 0xB8));
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);

    private static readonly IBrush PlayerFill = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));
    private static readonly IBrush PlayerHalo = new SolidColorBrush(Color.FromArgb(0x55, 0xF5, 0xC5, 0x42));
    private static readonly IPen PlayerOutline = new Pen(Brushes.Black, 1.5, lineJoin: PenLineJoin.Round);

    /// <summary>An arrowhead pointing up (north) around the origin, its tip ahead of the character.</summary>
    private static readonly Geometry PlayerArrow = Geometry.Parse("M 0,-11 L 7,8 L 0,4 L -7,8 Z");

    /// <summary>Icons by URL, shared by every map; loaded once through <see cref="AppServices.Icons"/>.</summary>
    private static readonly Dictionary<string, Bitmap> LoadedIcons = new();
    private static readonly HashSet<string> RequestedIcons = new();

    /// <summary>Each label's outlined text, built once at the origin and moved into place when drawn.</summary>
    private readonly Dictionary<MapAreaLabel, LabelText> _labelTexts = new();

    /// <summary>What the latest render drew that the pointer can hover, in drawing order.</summary>
    private readonly List<(Rect Area, MapMarker Marker)> _hitTargets = new();

    private MapMarker? _hovered;
    private bool _playerHovered;
    private Rect? _playerArea;
    private Point _hoveredAt;

    public MapLandmarks? Landmarks
    {
        get => GetValue(LandmarksProperty);
        set => SetValue(LandmarksProperty, value);
    }

    /// <summary>The character playing, drawn as an arrow the way they face.</summary>
    public MapPlayer? Player
    {
        get => GetValue(PlayerProperty);
        set => SetValue(PlayerProperty, value);
    }

    /// <summary>
    /// The view follows <see cref="Player"/>; dragging the map turns it off, zooming keeps it in the middle.
    /// </summary>
    public bool IsFollowing
    {
        get => GetValue(IsFollowingProperty);
        set => SetValue(IsFollowingProperty, value);
    }

    /// <summary>Run with the clicked <see cref="MapMarker"/>, when it has a chat link.</summary>
    public ICommand? CopyChatLinkCommand
    {
        get => GetValue(CopyChatLinkCommandProperty);
        set => SetValue(CopyChatLinkCommandProperty, value);
    }

    private void DrawLandmarks(DrawingContext context, Rect bounds, double left, double top, double scale)
    {
        _hitTargets.Clear();
        if (Landmarks is not { } landmarks)
        {
            return;
        }

        double zoom = Zoom;
        var drawnLabels = new List<Rect>();
        Point ToScreen(double x, double y) => new((x - left) * scale, (y - top) * scale);

        foreach (MapAreaLabel label in landmarks.Labels)
        {
            if (!IsShown(label.Kind, zoom))
            {
                continue;
            }

            Point at = ToScreen(label.X, label.Y);
            if (!IsNear(bounds, at, 200))
            {
                continue;
            }

            // Labels come most important first; one that would cover a label already drawn is left out.
            LabelText text = TextFor(label);
            Rect area = LabelArea(text, at);
            if (drawnLabels.Any(drawn => drawn.Intersects(area)))
            {
                continue;
            }

            drawnLabels.Add(area);
            DrawLabel(context, text, at);
        }

        foreach (MapMarker marker in landmarks.Markers)
        {
            if (zoom < (marker.Kind == MapMarkerKind.Waypoint ? WaypointsFromZoom : MarkersFromZoom))
            {
                continue;
            }

            Point at = ToScreen(marker.X, marker.Y);
            if (!IsNear(bounds, at, 20))
            {
                continue;
            }

            double size = marker.Kind == MapMarkerKind.Waypoint ? 24 : 20;
            Rect area = new(at.X - size / 2, at.Y - size / 2, size, size);
            if (IconFor(marker) is { } icon)
            {
                context.DrawImage(icon, new Rect(icon.Size), area);
            }
            else
            {
                context.DrawEllipse(PendingIcon, null, at, 4, 4);
            }

            _hitTargets.Add((area, marker));
        }

        DrawPlayer(context, left, top, scale);
        DrawTooltip(context, bounds);
    }

    private static bool IsShown(MapAreaLabelKind kind, double zoom) =>
        kind switch
        {
            MapAreaLabelKind.Region => zoom < RegionLabelsBelowZoom,
            MapAreaLabelKind.Map => zoom >= MapLabelsFromZoom && zoom < MapLabelsBelowZoom,
            _ => zoom >= SectorLabelsFromZoom,
        };

    private static bool IsNear(Rect bounds, Point point, double margin) =>
        point.X >= -margin
        && point.Y >= -margin
        && point.X <= bounds.Width + margin
        && point.Y <= bounds.Height + margin;

    /// <summary>The area a label centred on <paramref name="at"/> takes, its detail line included.</summary>
    private static Rect LabelArea(LabelText text, Point at)
    {
        double width = Math.Max(text.Main.Width, text.Detail?.Width ?? 0);
        double height = text.Main.Height + (text.Detail?.Height ?? 0);
        return new Rect(at.X - width / 2, at.Y - height / 2, width, height);
    }

    /// <summary>Draws a label centred on <paramref name="at"/>, with its detail line under it.</summary>
    private static void DrawLabel(DrawingContext context, LabelText text, Point at)
    {
        double y = at.Y - (text.Main.Height + (text.Detail?.Height ?? 0)) / 2;
        DrawOutlined(context, text.Main, text.Brush, at.X - text.Main.Width / 2, y);
        if (text.Detail is { } detail)
        {
            DrawOutlined(context, detail, DetailBrush, at.X - detail.Width / 2, y + text.Main.Height);
        }
    }

    private static void DrawOutlined(DrawingContext context, OutlinedText text, IBrush brush, double x, double y)
    {
        if (text.Geometry is { } geometry)
        {
            using (context.PushTransform(Matrix.CreateTranslation(Math.Round(x), Math.Round(y))))
            {
                context.DrawGeometry(null, TextOutline, geometry);
                context.DrawGeometry(brush, null, geometry);
            }
        }
    }

    private LabelText TextFor(MapAreaLabel label)
    {
        if (_labelTexts.TryGetValue(label, out LabelText? cached))
        {
            return cached;
        }

        LabelText text = label.Kind switch
        {
            MapAreaLabelKind.Region => new LabelText(Outline(label.Text, 22, FontWeight.SemiBold), null, RegionBrush),
            MapAreaLabelKind.Map => new LabelText(
                Outline(label.Text, 15, FontWeight.SemiBold),
                label.Detail is null ? null : Outline(label.Detail, 11, FontWeight.Normal),
                MapBrush
            ),
            _ => new LabelText(Outline(label.Text, 12, FontWeight.Normal), null, SectorBrush),
        };
        _labelTexts[label] = text;
        return text;
    }

    private static OutlinedText Outline(string text, double size, FontWeight weight)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, weight),
            size,
            Brushes.White
        );
        return new OutlinedText(formatted.BuildGeometry(new Point(0, 0)), formatted.Width, formatted.Height);
    }

    private Bitmap? IconFor(MapMarker marker)
    {
        string url = marker.Kind switch
        {
            MapMarkerKind.Waypoint => WaypointIcon,
            MapMarkerKind.Vista => VistaIcon,
            MapMarkerKind.Heart => HeartIcon,
            MapMarkerKind.HeroPoint => HeroPointIcon,
            MapMarkerKind.Unlock => marker.IconUrl ?? LandmarkIcon,
            _ => LandmarkIcon,
        };

        lock (LoadedIcons)
        {
            if (LoadedIcons.TryGetValue(url, out Bitmap? icon))
            {
                return icon;
            }

            if (!RequestedIcons.Add(url))
            {
                return null;
            }
        }

        _ = LoadIconAsync(url);
        return null;
    }

    private async Task LoadIconAsync(string url)
    {
        Bitmap? icon = AppServices.Icons is { } icons ? await icons.GetIconByUrlAsync(url) : null;
        lock (LoadedIcons)
        {
            if (icon is null)
            {
                // Retried the next time it's drawn.
                RequestedIcons.Remove(url);
                return;
            }

            LoadedIcons[url] = icon;
        }

        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    /// <summary>The name of what's under the pointer, and whether a click copies its chat link.</summary>
    private void DrawTooltip(DrawingContext context, Rect bounds)
    {
        (string? name, string? chatLink) =
            _playerHovered && Player is { } player ? (player.Name, null)
            : _hovered is { } marker ? (marker.Name, marker.ChatLink)
            : (null, null);
        if (name is null)
        {
            return;
        }

        FormattedText title = Text(name, 13, FontWeight.SemiBold, Brushes.White);
        FormattedText? hint = chatLink is null
            ? null
            : Text($"Click to copy {chatLink}", 11, FontWeight.Normal, TooltipHint);
        const double padding = 8;
        double width = Math.Max(title.Width, hint?.Width ?? 0) + padding * 2;
        double height = title.Height + (hint?.Height ?? 0) + padding * 2;
        double x = Math.Min(_hoveredAt.X + 14, bounds.Width - width - 4);
        double y = _hoveredAt.Y + 18 + height > bounds.Height ? _hoveredAt.Y - height - 8 : _hoveredAt.Y + 18;

        context.DrawRectangle(TooltipBackground, null, new Rect(x, y, width, height), 4, 4);
        context.DrawText(title, new Point(x + padding, y + padding));
        if (hint is not null)
        {
            context.DrawText(hint, new Point(x + padding, y + padding + title.Height));
        }
    }

    private static FormattedText Text(string text, double size, FontWeight weight, IBrush brush) =>
        new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, weight),
            size,
            brush
        );

    /// <summary>
    /// Finds what's under the pointer (the topmost drawn), and redraws when that or the tooltip's spot changes.
    /// </summary>
    private void UpdateHover(Point point)
    {
        bool playerHovered = _playerArea is { } playerArea && playerArea.Contains(point);
        if (playerHovered || _playerHovered)
        {
            _playerHovered = playerHovered;
            _hovered = null;
            _hoveredAt = point;
            Cursor = null;
            InvalidateVisual();
            if (playerHovered)
            {
                return;
            }
        }

        MapMarker? hovered = null;
        for (int i = _hitTargets.Count - 1; i >= 0; i--)
        {
            if (_hitTargets[i].Area.Contains(point))
            {
                hovered = _hitTargets[i].Marker;
                break;
            }
        }

        if (hovered is null && _hovered is null)
        {
            return;
        }

        _hovered = hovered;
        _hoveredAt = point;
        Cursor = HasChatLink(hovered) ? HandCursor : null;
        InvalidateVisual();
    }

    private void ClearHover()
    {
        if (_hovered is null && !_playerHovered)
        {
            return;
        }

        _hovered = null;
        _playerHovered = false;
        Cursor = null;
        InvalidateVisual();
    }

    private void ActivateHovered()
    {
        if (HasChatLink(_hovered) && CopyChatLinkCommand is { } command && command.CanExecute(_hovered))
        {
            command.Execute(_hovered);
        }
    }

    private static bool HasChatLink(MapMarker? marker) => marker?.ChatLink is not null;

    /// <summary>
    /// The character as an arrow pointing the way they face, over a soft halo so it stands out on any art.
    /// </summary>
    private void DrawPlayer(DrawingContext context, double left, double top, double scale)
    {
        _playerArea = null;
        if (Player is not { } player)
        {
            return;
        }

        Point at = new((player.X - left) * scale, (player.Y - top) * scale);
        context.DrawEllipse(PlayerHalo, null, at, 16, 16);
        using (context.PushTransform(Matrix.CreateRotation(player.Heading) * Matrix.CreateTranslation(at.X, at.Y)))
        {
            context.DrawGeometry(PlayerFill, PlayerOutline, PlayerArrow);
        }

        _playerArea = new Rect(at.X - 14, at.Y - 14, 28, 28);
    }

    private sealed record OutlinedText(Geometry? Geometry, double Width, double Height);

    private sealed record LabelText(OutlinedText Main, OutlinedText? Detail, IBrush Brush);
}
