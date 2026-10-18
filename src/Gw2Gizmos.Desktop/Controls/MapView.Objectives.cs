using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// WvW objectives: the game's icon for each on a disc in its holder's colour, pips under it for the tiers its yaks have
/// built up, a gold dot when its guild slotted upgrades, a countdown above one still protected after a flip (it can't
/// be retaken for five minutes), and the claiming guild's tag below once zoomed in. Hovering shows who holds it, since
/// when, how built up it is and what it earns; clicking copies its chat link.
/// </summary>
public sealed partial class MapView
{
    public static readonly StyledProperty<IReadOnlyList<WvwObjectiveMarker>?> ObjectivesProperty =
        AvaloniaProperty.Register<MapView, IReadOnlyList<WvwObjectiveMarker>?>(nameof(Objectives));

    /// <summary>Guild tags show under objectives from this zoom.</summary>
    private const double GuildTagsFromZoom = 4.5;

    private static readonly IBrush RedSide = new SolidColorBrush(Color.FromRgb(0xD9, 0x53, 0x4F));
    private static readonly IBrush BlueSide = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
    private static readonly IBrush GreenSide = new SolidColorBrush(Color.FromRgb(0x5C, 0xB8, 0x5C));
    private static readonly IBrush NeutralSide = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    private static readonly IPen DiscOutline = new Pen(
        new SolidColorBrush(Color.FromArgb(0xE0, 0x00, 0x00, 0x00)),
        1.5
    );
    private static readonly IPen ProtectedRing = new Pen(Brushes.White, 2);
    private static readonly IBrush TierReached = Brushes.White;
    private static readonly IBrush TierAhead = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00));
    private static readonly IBrush GuildUpgradesDot = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));

    /// <summary>The yak tiers an objective can reach: secured, reinforced, fortified.</summary>
    private const int TierCount = 3;

    /// <summary>The objectives to draw.</summary>
    public IReadOnlyList<WvwObjectiveMarker>? Objectives
    {
        get => GetValue(ObjectivesProperty);
        set => SetValue(ObjectivesProperty, value);
    }

    private void DrawObjectives(DrawingContext context, Rect bounds, double left, double top, double scale)
    {
        if (Objectives is not { Count: > 0 } objectives)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (WvwObjectiveMarker objective in objectives)
        {
            Point at = new((objective.X - left) * scale, (objective.Y - top) * scale);
            if (!IsNear(bounds, at, 40))
            {
                continue;
            }

            double radius = objective.Type switch
            {
                "Castle" => 15,
                "Keep" => 13,
                "Tower" => 12,
                _ => 10,
            };
            TimeSpan? protectedFor = objective.ProtectedFor(now);
            context.DrawEllipse(
                SideBrush(objective.Owner),
                protectedFor is null ? DiscOutline : ProtectedRing,
                at,
                radius,
                radius
            );
            if (objective.IconUrl is { } url && Icon(url) is { } icon)
            {
                // The icon fills most of the disc, leaving a thin ring of the holder's colour.
                double size = radius * 1.9;
                context.DrawImage(icon, new Rect(icon.Size), new Rect(at.X - size / 2, at.Y - size / 2, size, size));
            }

            DrawTierPips(context, at, radius, objective.Tier);
            if (objective.GuildUpgrades is { Count: > 0 })
            {
                // Top-right on the rim, like a notification dot.
                Point dot = new(at.X + radius * 0.72, at.Y - radius * 0.72);
                context.DrawEllipse(GuildUpgradesDot, DiscOutline, dot, 4, 4);
            }

            if (protectedFor is { } protectedLeft)
            {
                DrawPill(
                    context,
                    $"{protectedLeft.Minutes}:{protectedLeft.Seconds:00}",
                    new Point(at.X, at.Y - radius - 10)
                );
            }

            if (Zoom >= GuildTagsFromZoom && objective.Guild is { } guild)
            {
                DrawPill(context, $"[{guild.Tag}]", new Point(at.X, at.Y + radius + 16));
            }

            _hitTargets.Add((new Rect(at.X - radius, at.Y - radius, radius * 2, radius * 2), objective));
        }
    }

    /// <summary>Three pips along the disc's bottom, the reached tiers filled.</summary>
    private static void DrawTierPips(DrawingContext context, Point at, double radius, int tier)
    {
        const double pipRadius = 2.5;
        const double spacing = 7;
        for (int i = 0; i < TierCount; i++)
        {
            Point pip = new(at.X + (i - 1) * spacing, at.Y + radius + 5);
            context.DrawEllipse(i < tier ? TierReached : TierAhead, DiscOutline, pip, pipRadius, pipRadius);
        }
    }

    /// <summary>A small dark label centred on <paramref name="center"/>.</summary>
    private static void DrawPill(DrawingContext context, string text, Point center)
    {
        FormattedText formatted = Text(text, 11, FontWeight.SemiBold, Brushes.White);
        Rect pill = new(
            Math.Round(center.X - formatted.Width / 2 - 4),
            Math.Round(center.Y - formatted.Height / 2 - 1),
            formatted.Width + 8,
            formatted.Height + 2
        );
        context.DrawRectangle(BadgeBackground, null, pill, 3, 3);
        context.DrawText(formatted, new Point(pill.X + 4, pill.Y + 1));
    }

    /// <summary>
    /// Tooltip lines for an objective: who holds it and since when, who claimed it, how built up it is, what it earns
    /// and the guild's upgrades.
    /// </summary>
    private static IReadOnlyList<FormattedText> ObjectiveLines(WvwObjectiveMarker objective)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var lines = new List<FormattedText>
        {
            Text(
                objective.OwnerName is { } owner ? $"{objective.Type} · {owner}" : $"{objective.Type} · neutral",
                12,
                FontWeight.Normal,
                SideBrush(objective.Owner)
            ),
        };

        string flipped = objective.ProtectedFor(now) is { } left
            ? $"Can't be retaken for {left.Minutes}:{left.Seconds:00}"
            : $"Held for {Ago(now - objective.LastFlipped)}";
        lines.Add(Text(flipped, 12, FontWeight.Normal, LaterText));
        if (objective.Guild is { } guild)
        {
            lines.Add(Text($"Claimed by {guild.Name} [{guild.Tag}]", 12, FontWeight.Normal, LaterText));
        }

        string tier = (objective.TierName, objective.NextTierName, objective.YaksToNextTier) switch
        {
            (null, { } next, { } yaks) => $"Not upgraded · {Yaks(yaks)} to {next}",
            ({ } reached, { } next, { } yaks) => $"{reached} · {Yaks(yaks)} more to {next}",
            ({ } reached, null, _) => reached,
            _ => "",
        };
        if (tier.Length > 0)
        {
            lines.Add(Text(tier, 12, FontWeight.Normal, LaterText));
        }

        string earns =
            objective.YaksDelivered > 0
                ? $"+{objective.PointsTick} per tick · {objective.YaksDelivered} yaks delivered"
                : $"+{objective.PointsTick} per tick";
        lines.Add(Text(earns, 12, FontWeight.Normal, LaterText));
        if (objective.GuildUpgrades is { Count: > 0 } slotted)
        {
            lines.Add(Text("Guild upgrades", 12, FontWeight.SemiBold, SoonText));
            lines.AddRange(slotted.Select(name => Text($"• {name}", 12, FontWeight.Normal, LaterText)));
        }

        return lines;
    }

    private static string Yaks(int count) => count == 1 ? "1 yak" : $"{count} yaks";

    private static string Ago(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{Math.Max(0, span.Minutes)} min";

    private static IBrush SideBrush(WvwColor? side) =>
        side switch
        {
            WvwColor.Red => RedSide,
            WvwColor.Blue => BlueSide,
            WvwColor.Green => GreenSide,
            _ => NeutralSide,
        };

    /// <summary>Whether any objective is still protected after a flip, so its countdown needs ticking.</summary>
    private bool HasProtectedObjectives()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return Objectives?.Any(o => o.ProtectedFor(now) is not null) == true;
    }
}
