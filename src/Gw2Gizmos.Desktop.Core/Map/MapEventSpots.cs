using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>The scheduled events that start from one waypoint, placed where it is on the map.</summary>
/// <param name="WaypointName">The waypoint's (or point of interest's) name, from the map.</param>
public sealed record MapEventSpot(
    double X,
    double Y,
    string ChatLink,
    string WaypointName,
    IReadOnlyList<ScheduledEvent> Events
)
{
    /// <summary>The events in order of how pressing they are: active ones first, then the soonest to start.</summary>
    public IReadOnlyList<(ScheduledEvent Event, EventTiming Timing)> ByUrgency(DateTimeOffset now) =>
        Events
            .Select(e => (Event: e, Timing: EventTiming.Of(e, now)))
            .OrderByDescending(e => e.Timing.IsActive)
            .ThenBy(e => e.Timing.SortKey)
            .ToList();
}

public static class MapEventSpots
{
    /// <summary>
    /// Places events at the map marker their chat link names, one spot per waypoint holding every event there. Events
    /// without a chat link, or with one the map doesn't have, are left out.
    /// </summary>
    public static IReadOnlyList<MapEventSpot> Build(IEnumerable<ScheduledEvent> events, MapLandmarks landmarks)
    {
        var markers = new Dictionary<string, MapMarker>();
        foreach (MapMarker marker in landmarks.Markers)
        {
            if (marker.ChatLink is { } link)
            {
                markers.TryAdd(link, marker);
            }
        }

        return events
            .Where(e => e.ChatLink is not null)
            .GroupBy(e => e.ChatLink!)
            .Select(group =>
                markers.TryGetValue(group.Key, out MapMarker? marker)
                    ? new MapEventSpot(marker.X, marker.Y, group.Key, marker.Name, group.ToList())
                    : null
            )
            .OfType<MapEventSpot>()
            .ToList();
    }
}
