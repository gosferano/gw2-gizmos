using Gw2Gizmos.Data.Static.Events;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Pinning scheduled events at their waypoints on the world map.</summary>
public sealed class MapEventSpotsTests
{
    private static ScheduledEvent Event(string id, string? chatLink, params int[] hoursUtc) =>
        new()
        {
            Id = id,
            Name = id,
            Map = "Somewhere",
            Kind = EventKind.MetaEvent,
            Expansion = Expansion.CoreTyria,
            ChatLink = chatLink,
            DailyTimesUtc = hoursUtc.Select(h => TimeSpan.FromHours(h)).ToArray(),
        };

    private static readonly MapLandmarks Landmarks = new(
        [],
        [
            new MapMarker(MapMarkerKind.Waypoint, "Aerodrome Waypoint", 10, 20, "[&wp1]"),
            new MapMarker(MapMarkerKind.Waypoint, "Shore Waypoint", 30, 40, "[&wp2]"),
        ]
    );

    [Fact]
    public void Events_sharing_a_waypoint_make_one_spot_there_and_ones_the_map_lacks_are_left_out()
    {
        ScheduledEvent[] events =
        [
            Event("marionette", "[&wp1]", 1),
            Event("dragonstorm", "[&wp1]", 2),
            Event("shore", "[&wp2]", 3),
            Event("nowhere", "[&unknown]", 4),
            Event("everywhere", null, 5),
        ];

        IReadOnlyList<MapEventSpot> spots = MapEventSpots.Build(events, Landmarks);

        Assert.Equal(2, spots.Count);
        MapEventSpot aerodrome = spots.Single(s => s.ChatLink == "[&wp1]");
        Assert.Equal(("Aerodrome Waypoint", 10.0, 20.0), (aerodrome.WaypointName, aerodrome.X, aerodrome.Y));
        Assert.Equal(["marionette", "dragonstorm"], aerodrome.Events.Select(e => e.Id));
    }

    [Fact]
    public void A_spots_events_come_active_first_then_soonest()
    {
        MapEventSpot spot = new(
            0,
            0,
            "[&wp1]",
            "Aerodrome Waypoint",
            [Event("later", "[&wp1]", 15), Event("soon", "[&wp1]", 13), Event("active", "[&wp1]", 12)]
        );
        DateTimeOffset now = new(2026, 10, 18, 12, 5, 0, TimeSpan.Zero);

        Assert.Equal(["active", "soon", "later"], spot.ByUrgency(now).Select(e => e.Event.Id));
        Assert.True(spot.ByUrgency(now)[0].Timing.IsActive);
    }
}
