using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Maps;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>The maps sync stores /v2/maps (rectangles and floors as JSON) and updates rows it has seen before.</summary>
public sealed class MapsUpdaterTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();
    private readonly RoutingHttpStub _stub = new();

    public void Dispose() => _fixture.Dispose();

    private MapsUpdater NewUpdater(Gw2GizmosDbContext db) =>
        new(db, _stub.BuildClientFactory(), NullLogger<MapsUpdater>.Instance);

    private static string MapJson(int id, string name) =>
        $$"""
        {
          "id": {{id}}, "name": "{{name}}", "min_level": 80, "max_level": 80, "default_floor": 1, "type": "Public",
          "floors": [1, 3, 49], "region_id": 37, "region_name": "Cantha", "continent_id": 1, "continent_name": "Tyria",
          "map_rect": [[-30720, -22528], [30720, 22528]], "continent_rect": [[29824, 101248], [33664, 104064]]
        }
        """;

    [Fact]
    public async Task Stores_every_map_with_its_rectangles_and_floors()
    {
        _stub.SetJson("/v2/maps", $"[{MapJson(1422, "Dragon's End")}, {MapJson(50, "Lion's Arch")}]");

        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            await NewUpdater(db).UpdateMaps(CancellationToken.None);
        }

        Map map = await _fixture.NewContext().Maps.SingleAsync(m => m.Id == 1422);
        Assert.Equal("Dragon's End", map.Name);
        Assert.Equal("Public", map.Type);
        Assert.Equal("Cantha", map.RegionName);
        Assert.Equal([1, 3, 49], map.Floors);
        Assert.Equal((-30720, -22528, 30720, 22528), (map.MapRect.Min.X, map.MapRect.Min.Y, map.MapRect.Max.X, map.MapRect.Max.Y));
        Assert.Equal((29824, 104064), (map.ContinentRect.Min.X, map.ContinentRect.Max.Y));
        Assert.Equal(2, await _fixture.NewContext().Maps.CountAsync());
    }

    [Fact]
    public async Task Trims_names_the_API_pads_with_whitespace()
    {
        // As the API returns WvW's Desert Borderlands.
        _stub.SetJson("/v2/maps", $"[{MapJson(1099, " Desert Borderlands")}]");

        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            await NewUpdater(db).UpdateMaps(CancellationToken.None);
        }

        Assert.Equal("Desert Borderlands", (await _fixture.NewContext().Maps.SingleAsync()).Name);
    }

    [Fact]
    public async Task A_later_sync_updates_existing_maps_and_keeps_ones_no_longer_listed()
    {
        _stub.SetJson("/v2/maps", $"[{MapJson(1422, "Old Name")}, {MapJson(50, "Lion's Arch")}]");
        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            await NewUpdater(db).UpdateMaps(CancellationToken.None);
        }

        _stub.SetJson("/v2/maps", $"[{MapJson(1422, "Dragon's End")}]");
        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            await NewUpdater(db).UpdateMaps(CancellationToken.None);
        }

        Gw2GizmosDbContext read = _fixture.NewContext();
        Assert.Equal("Dragon's End", (await read.Maps.SingleAsync(m => m.Id == 1422)).Name);
        Assert.True(await read.Maps.AnyAsync(m => m.Id == 50));
    }
}
