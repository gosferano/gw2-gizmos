using Gw2Gizmos.Gw2Api.Contract.V2.Continents;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Turning the API's continent floors into the world map's labels and markers.</summary>
public sealed class MapLandmarkProviderTests
{
    private static ContinentFloorRegionMap Map(int id, string name, params ContinentFloorRegionMapPoi[] pois) =>
        new()
        {
            Id = id,
            Name = name,
            MinLevel = 1,
            MaxLevel = 15,
            LabelCoord = [id * 10, id * 10],
            PointsOfInterest = pois.ToDictionary(p => p.Id),
        };

    private static ContinentFloorRegionMapPoi Waypoint(int id, string name) =>
        new()
        {
            Id = id,
            Name = name,
            Type = ContinentFloorRegionMapPoiType.Waypoint,
            Coord = [id, id],
            ChatLink = $"[&wp{id}]",
        };

    private static ContinentFloor Floor(
        int id,
        params (int Id, string Name, ContinentFloorRegionMap[] Maps)[] regions
    ) =>
        new()
        {
            Id = id,
            Regions = regions.ToDictionary(
                r => r.Id,
                r => new ContinentFloorRegion
                {
                    Id = r.Id,
                    Name = r.Name,
                    LabelCoord = [r.Id, r.Id],
                    Maps = r.Maps.ToDictionary(m => m.Id),
                }
            ),
        };

    [Fact]
    public void A_map_only_on_a_later_floor_is_added_but_one_already_seen_is_not_taken_again()
    {
        ContinentFloor ground = Floor(1, (4, "Kryta", [Map(15, "Queensdale", Waypoint(1, "Shaemoor Waypoint"))]));
        ContinentFloor elona = Floor(
            49,
            (4, "Kryta", [Map(15, "Queensdale", Waypoint(99, "Duplicate"))]),
            (12, "Crystal Desert", [Map(1210, "Crystal Oasis", Waypoint(2, "Amnoon Waypoint"))])
        );

        MapLandmarks landmarks = MapLandmarkProvider.Build([ground, elona], explorable: null);

        Assert.Equal(["Shaemoor Waypoint", "Amnoon Waypoint"], landmarks.Markers.Select(m => m.Name));
        Assert.Equal(["Kryta", "Crystal Desert", "Queensdale", "Crystal Oasis"], landmarks.Labels.Select(l => l.Text));
    }

    [Fact]
    public void A_region_is_labelled_where_the_floor_holding_most_of_its_maps_puts_it()
    {
        // Floor 1's Crystal Desert holds only Istan and labels it there; floor 49 holds Elona and labels it in Elona.
        ContinentFloor ground = Floor(1, (12, "Crystal Desert", [Map(1263, "Domain of Istan")]));
        ground.Regions[12].LabelCoord = [100, 100];
        ContinentFloor elona = Floor(
            49,
            (12, "Crystal Desert", [Map(1210, "Crystal Oasis"), Map(1211, "Desert Highlands")])
        );
        elona.Regions[12].LabelCoord = [500, 500];

        MapLandmarks landmarks = MapLandmarkProvider.Build([ground, elona], explorable: null);

        MapAreaLabel region = landmarks.Labels.Single(l => l.Kind == MapAreaLabelKind.Region);
        Assert.Equal((500.0, 500.0), (region.X, region.Y));
    }

    [Fact]
    public void Map_labels_come_largest_map_first_then_by_id_without_the_public_suffix()
    {
        ContinentFloorRegionMap small = Map(20, "Small");
        small.ContinentRect =
        [
            [0, 0],
            [10, 10],
        ];
        ContinentFloorRegionMap pass = Map(1331, "Shiverpeaks Pass (Public)");
        pass.ContinentRect =
        [
            [0, 0],
            [100, 100],
        ];
        ContinentFloorRegionMap grothmar = Map(1330, "Grothmar Valley");
        grothmar.ContinentRect =
        [
            [0, 0],
            [100, 100],
        ];
        ContinentFloor floor = Floor(1, (4, "Ascalon", [small, pass, grothmar]));

        MapLandmarks landmarks = MapLandmarkProvider.Build([floor], explorable: null);

        Assert.Equal(
            ["Grothmar Valley", "Shiverpeaks Pass", "Small"],
            landmarks.Labels.Where(l => l.Kind == MapAreaLabelKind.Map).Select(l => l.Text)
        );
    }

    [Fact]
    public void Only_explorable_maps_count_and_a_region_without_any_gets_no_label()
    {
        ContinentFloor floor = Floor(
            1,
            (4, "Kryta", [Map(15, "Queensdale", Waypoint(1, "Shaemoor Waypoint")), Map(900, "A story instance")]),
            (7, "Instances only", [Map(901, "Another instance", Waypoint(2, "Hidden Waypoint"))])
        );

        MapLandmarks landmarks = MapLandmarkProvider.Build([floor], explorable: [15]);

        Assert.Equal(["Kryta", "Queensdale"], landmarks.Labels.Select(l => l.Text));
        Assert.Equal(["Shaemoor Waypoint"], landmarks.Markers.Select(m => m.Name));
    }

    [Fact]
    public void A_point_of_interest_several_maps_share_is_kept_once()
    {
        ContinentFloorRegionMapPoi shared = Waypoint(1, "Shared Waypoint");
        ContinentFloor floor = Floor(1, (4, "Kryta", [Map(15, "Queensdale", shared), Map(16, "Overlay", shared)]));

        MapLandmarks landmarks = MapLandmarkProvider.Build([floor], explorable: null);

        MapMarker marker = Assert.Single(landmarks.Markers);
        Assert.Equal(MapMarkerKind.Waypoint, marker.Kind);
        Assert.Equal("[&wp1]", marker.ChatLink);
    }

    [Fact]
    public void Maps_get_their_level_range_and_sectors_lose_the_game_markup_and_their_chat_link()
    {
        ContinentFloorRegionMap map = Map(15, "Queensdale");
        map.Sectors[5] = new ContinentFloorRegionMapSector
        {
            Id = 5,
            Name = "<c=@reminder>Shaemoor</c>",
            Coord = [1, 2],
            ChatLink = "[&BAUAAAA=]",
        };
        ContinentFloor floor = Floor(1, (4, "Kryta", [map]));

        MapLandmarks landmarks = MapLandmarkProvider.Build([floor], explorable: null);

        Assert.Equal("1–15", landmarks.Labels.Single(l => l.Kind == MapAreaLabelKind.Map).Detail);
        MapAreaLabel sector = landmarks.Labels.Single(l => l.Kind == MapAreaLabelKind.Sector);
        Assert.Equal("Shaemoor", sector.Text);
    }

    [Fact]
    public void Raids_and_guild_halls_are_labelled_as_such_at_their_middle_over_an_explorable_map_of_the_name()
    {
        ContinentFloor floor = Floor(1, (5, "Heart of Maguuma", [Map(1147, "Spirit Vale")]));
        MapLandmarkProvider.NotableMap[] notable =
        [
            new(1062, "Spirit Vale", "Raid", 0, 0, 10, 10),
            new(1323, "The Key of Ahdashim", "Raid", 100, 200, 300, 400),
        ];

        MapLandmarks landmarks = MapLandmarkProvider.Build([floor], explorable: null, notable);

        MapAreaLabel[] maps = landmarks.Labels.Where(l => l.Kind == MapAreaLabelKind.Map).ToArray();
        Assert.Equal(["Spirit Vale", "The Key of Ahdashim"], maps.Select(l => l.Text).Order());
        MapAreaLabel key = maps.Single(l => l.Text == "The Key of Ahdashim");
        Assert.Equal((200.0, 300.0, "Raid"), (key.X, key.Y, key.Detail));
        MapAreaLabel vale = maps.Single(l => l.Text == "Spirit Vale");
        Assert.Equal((5.0, 5.0, "Raid"), (vale.X, vale.Y, vale.Detail));
    }

    [Fact]
    public void A_label_point_outside_its_own_map_moves_to_the_middle_of_the_map()
    {
        ContinentFloorRegionMap map = Map(1147, "Spirit Vale");
        map.ContinentRect =
        [
            [36224, 27396],
            [37504, 30596],
        ];
        map.LabelCoord = [35968, 29440];
        ContinentFloor floor = Floor(1, (5, "Heart of Maguuma", [map]));

        MapLandmarks landmarks = MapLandmarkProvider.Build([floor], explorable: null);

        MapAreaLabel label = landmarks.Labels.Single(l => l.Kind == MapAreaLabelKind.Map);
        Assert.Equal((36864.0, 28996.0), (label.X, label.Y));
    }
}
