using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>What a map marker stands for.</summary>
public enum MapMarkerKind
{
    Waypoint,
    Landmark,
    Vista,

    /// <summary>A point of interest with its own icon, such as a dungeon or raid entrance.</summary>
    Unlock,

    /// <summary>A renown heart; it has no chat link (the API's are another point of interest's).</summary>
    Heart,

    /// <summary>A hero challenge; it has no chat link.</summary>
    HeroPoint,
}

/// <summary>An icon on the map, at a continent point.</summary>
/// <param name="ChatLink">The code to paste in game, when it has one.</param>
/// <param name="IconUrl">The icon to draw, for kinds without a standard one.</param>
public sealed record MapMarker(
    MapMarkerKind Kind,
    string Name,
    double X,
    double Y,
    string? ChatLink = null,
    string? IconUrl = null
);

/// <summary>What a map label names.</summary>
public enum MapAreaLabelKind
{
    Region,
    Map,

    /// <summary>A named area within a map.</summary>
    Sector,
}

/// <summary>A name written on the map, centred on a continent point.</summary>
/// <param name="Detail">A second line, such as a map's level range.</param>
public sealed record MapAreaLabel(MapAreaLabelKind Kind, string Text, double X, double Y, string? Detail = null);

/// <summary>Everything written and pinned on a continent's map.</summary>
public sealed record MapLandmarks(IReadOnlyList<MapAreaLabel> Labels, IReadOnlyList<MapMarker> Markers)
{
    public static MapLandmarks Empty { get; } = new(Array.Empty<MapAreaLabel>(), Array.Empty<MapMarker>());
}

/// <summary>The character playing, on the map.</summary>
/// <param name="Heading">The way they face, in radians clockwise from north.</param>
public sealed record MapPlayer(string Name, double X, double Y, double Heading)
{
    /// <summary>The character in <paramref name="game"/>, if they're on <paramref name="continent"/>'s map.</summary>
    public static MapPlayer? On(MapContinent continent, LiveGameState game) =>
        game.Position is { } at && at.ContinentId == continent.Id && game.Character is { } name
            ? new MapPlayer(name, at.X, at.Y, at.Heading)
            : null;
}

/// <summary>
/// A spot for the map to glide to, rather than jump. A class, not a record: asking for the same spot twice is two
/// requests, so each must differ from the last.
/// </summary>
public sealed class MapFlight(double x, double y, double zoom)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Zoom { get; } = zoom;
}
