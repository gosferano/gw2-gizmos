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

    /// <summary>A renown heart.</summary>
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
