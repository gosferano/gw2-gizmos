using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Continents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The labels and markers of a continent's map, from the API's continent floors, fetched by the desktop itself (public
/// data, no key) and kept in a file in the data folder for a week. Only explorable maps count — the floors also hold
/// every instance and story map drawn over them — and a landmark several maps share is kept once.
/// </summary>
public sealed class MapLandmarkProvider
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions FileOptions = new() { WriteIndented = false };

    private readonly IGw2ApiClientFactory _clients;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppPaths _paths;

    public MapLandmarkProvider(IGw2ApiClientFactory clients, IServiceScopeFactory scopeFactory, AppPaths paths)
    {
        _clients = clients;
        _scopeFactory = scopeFactory;
        _paths = paths;
    }

    /// <summary>
    /// The continent's landmarks: from the file while it's fresh, otherwise from the API (falling back to an old file,
    /// or nothing, when the API can't be reached).
    /// </summary>
    public async Task<MapLandmarks> LoadAsync(MapContinent continent)
    {
        string path = _paths.File(Path.Combine("MapCache", $"landmarks-{continent.Id}-{continent.Floor}.json"));
        MapLandmarks? cached = await ReadAsync(path);
        if (cached is not null && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < MaxAge)
        {
            return cached;
        }

        try
        {
            MapLandmarks fetched = await FetchAsync(continent);
            await WriteAsync(path, fetched);
            return fetched;
        }
        catch (Exception)
        {
            // Offline or the API is down: an old file is better than an empty map.
            return cached ?? MapLandmarks.Empty;
        }
    }

    private async Task<MapLandmarks> FetchAsync(MapContinent continent)
    {
        Gw2ApiClient client = _clients.Create(Locale.English);
        int[] floorIds = [continent.Floor, .. continent.ExtraDataFloors];
        ContinentFloor[]? floors = await client.V2.Continents[continent.Id].Floors.GetByIds(floorIds);
        if (floors is null)
        {
            throw new InvalidOperationException("The continent floors couldn't be fetched.");
        }

        (HashSet<int>? explorable, List<NotableMap> notable) = await ReadMapCatalogAsync();
        return Build(floors.OrderBy(f => Array.IndexOf(floorIds, f.Id)), explorable, notable);
    }

    /// <summary>
    /// Turns floors into landmarks. A map on several floors is taken from the first; <paramref name="explorable"/>
    /// limits the maps to those ids, unless null (not known yet). A region's label is taken from the floor holding most
    /// of its maps (Crystal Desert's on floor 1 sits by Istan, its only map there). Map labels come largest map first,
    /// then by id, so where two share a spot the map drawing them keeps the bigger or older one (Shiverpeaks Pass has
    /// Grothmar Valley's very rectangle and label spot).
    /// </summary>
    internal static MapLandmarks Build(
        IEnumerable<ContinentFloor> floors,
        HashSet<int>? explorable,
        IReadOnlyList<NotableMap>? notable = null
    )
    {
        var regionLabels = new Dictionary<int, (int MapCount, MapAreaLabel Label)>();
        var mapLabels = new List<(double Area, int MapId, MapAreaLabel Label)>();
        var sectorLabels = new List<MapAreaLabel>();
        var markers = new List<MapMarker>();
        var seenMaps = new HashSet<int>();
        var seenMapLabels = new HashSet<(string, double, double)>();
        var notableNames = (notable ?? []).Select(m => m.Name).ToHashSet();
        var seenPois = new HashSet<int>();
        var seenHearts = new HashSet<int>();
        var seenSectors = new HashSet<int>();
        var seenHeroPoints = new HashSet<string>();

        foreach (ContinentFloor floor in floors)
        {
            foreach ((int regionId, ContinentFloorRegion region) in floor.Regions)
            {
                List<ContinentFloorRegionMap> maps = region
                    .Maps.Values.Where(m => explorable is null || explorable.Contains(m.Id))
                    .Where(m => seenMaps.Add(m.Id))
                    .ToList();
                if (maps.Count == 0)
                {
                    continue;
                }

                if (
                    region.LabelCoord.Length == 2
                    && (!regionLabels.TryGetValue(regionId, out var best) || maps.Count > best.MapCount)
                )
                {
                    regionLabels[regionId] = (
                        maps.Count,
                        new MapAreaLabel(
                            MapAreaLabelKind.Region,
                            region.Name,
                            region.LabelCoord[0],
                            region.LabelCoord[1]
                        )
                    );
                }

                foreach (ContinentFloorRegionMap map in maps)
                {
                    AddMap(map);
                }
            }
        }

        // Raids and guild halls, labelled at the middle of their map as such, even where an explorable map of the name
        // has a label of its own (Spirit Vale's points at Salvation Pass).
        foreach (NotableMap map in notable ?? [])
        {
            mapLabels.Add(
                (map.Area, map.Id, new MapAreaLabel(MapAreaLabelKind.Map, map.Name, map.CenterX, map.CenterY, map.Kind))
            );
        }

        List<MapAreaLabel> labels =
        [
            .. regionLabels.Values.Select(r => r.Label),
            .. mapLabels.OrderByDescending(m => m.Area).ThenBy(m => m.MapId).Select(m => m.Label),
            .. sectorLabels,
        ];
        return new MapLandmarks(labels, markers);

        void AddMap(ContinentFloorRegionMap map)
        {
            (double X, double Y)? spot = LabelSpot(map);
            if (spot is { } at && !notableNames.Contains(map.Name) && seenMapLabels.Add((map.Name, at.X, at.Y)))
            {
                string? levels =
                    map.MaxLevel > 0
                        ? map.MinLevel == map.MaxLevel
                            ? $"{map.MaxLevel}"
                            : $"{map.MinLevel}–{map.MaxLevel}"
                        : null;
                mapLabels.Add(
                    (
                        Area(map.ContinentRect),
                        map.Id,
                        new MapAreaLabel(MapAreaLabelKind.Map, WithoutPublicSuffix(map.Name), at.X, at.Y, levels)
                    )
                );
            }

            foreach (ContinentFloorRegionMapPoi poi in map.PointsOfInterest.Values)
            {
                if (poi.Coord.Length == 2 && seenPois.Add(poi.Id))
                {
                    markers.Add(
                        new MapMarker(
                            KindOf(poi.Type),
                            string.IsNullOrWhiteSpace(poi.Name) ? NameOf(poi.Type) : poi.Name,
                            (double)poi.Coord[0],
                            (double)poi.Coord[1],
                            poi.ChatLink,
                            poi.Icon
                        )
                    );
                }
            }

            // Hearts' chat links are left out, as sectors' are: the API makes them as point-of-interest links with the
            // heart's id, so in game they name whichever point of interest has that id (Queensdale's heart 228 gives
            // Provern Shore Waypoint in Gendarran Fields), and an event at that waypoint would be pinned to the heart.
            foreach (ContinentFloorRegionMapTask heart in map.Tasks.Values)
            {
                if (heart.Coord.Length == 2 && seenHearts.Add(heart.Id))
                {
                    markers.Add(
                        new MapMarker(
                            MapMarkerKind.Heart,
                            heart.Objective,
                            (double)heart.Coord[0],
                            (double)heart.Coord[1]
                        )
                    );
                }
            }

            foreach (ContinentFloorRegionMapSkillChallenge challenge in map.SkillChallenges)
            {
                if (challenge.Coord.Length == 2 && seenHeroPoints.Add(challenge.Id))
                {
                    markers.Add(
                        new MapMarker(
                            MapMarkerKind.HeroPoint,
                            "Hero challenge",
                            (double)challenge.Coord[0],
                            (double)challenge.Coord[1]
                        )
                    );
                }
            }

            // Sectors' chat links are left out: the API makes them as point-of-interest links with the sector's id,
            // so in game they name whichever point of interest has that id (Perimeter Loop's gives Skull Plaza).
            foreach (ContinentFloorRegionMapSector sector in map.Sectors.Values)
            {
                if (sector.Coord.Length == 2 && !string.IsNullOrWhiteSpace(sector.Name) && seenSectors.Add(sector.Id))
                {
                    sectorLabels.Add(
                        new MapAreaLabel(
                            MapAreaLabelKind.Sector,
                            StripMarkup(sector.Name),
                            (double)sector.Coord[0],
                            (double)sector.Coord[1]
                        )
                    );
                }
            }
        }
    }

    /// <summary>
    /// Where a map's name goes: its label point, unless that falls outside the map itself (Spirit Vale's sits on
    /// Salvation Pass), then the middle of the map.
    /// </summary>
    private static (double X, double Y)? LabelSpot(ContinentFloorRegionMap map)
    {
        int[][] rect = map.ContinentRect;
        bool hasRect = rect.Length == 2 && rect[0].Length == 2 && rect[1].Length == 2;
        if (map.LabelCoord.Length == 2)
        {
            (double x, double y) = (map.LabelCoord[0], map.LabelCoord[1]);
            if (!hasRect || (x >= rect[0][0] && x <= rect[1][0] && y >= rect[0][1] && y <= rect[1][1]))
            {
                return (x, y);
            }
        }

        return hasRect ? ((rect[0][0] + rect[1][0]) / 2.0, (rect[0][1] + rect[1][1]) / 2.0) : null;
    }

    private static double Area(int[][] rect) =>
        rect.Length == 2 && rect[0].Length == 2 && rect[1].Length == 2
            ? (double)(rect[1][0] - rect[0][0]) * (rect[1][1] - rect[0][1])
            : 0;

    /// <summary>
    /// "Shiverpeaks Pass (Public)": the suffix tells it from an instance of the same name, which isn't shown.
    /// </summary>
    private static string WithoutPublicSuffix(string name) =>
        name.EndsWith(" (Public)", StringComparison.Ordinal) ? name[..^" (Public)".Length] : name;

    private static MapMarkerKind KindOf(ContinentFloorRegionMapPoiType type) =>
        type == ContinentFloorRegionMapPoiType.Waypoint ? MapMarkerKind.Waypoint
        : type == ContinentFloorRegionMapPoiType.Vista ? MapMarkerKind.Vista
        : type == ContinentFloorRegionMapPoiType.Unlock ? MapMarkerKind.Unlock
        : MapMarkerKind.Landmark;

    private static string NameOf(ContinentFloorRegionMapPoiType type) =>
        type == ContinentFloorRegionMapPoiType.Vista ? "Vista" : "Point of interest";

    /// <summary>Some sector names carry the game's colour markup, e.g. <c>&lt;c=@...&gt;</c>.</summary>
    private static string StripMarkup(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "").Trim();

    /// <summary>
    /// Instance maps the game's world map labels all the same: the raids and the guild halls. They're instances, often
    /// on floors of their own, so the floors this map is drawn from don't list them; /v2/maps (the Maps table) does.
    /// </summary>
    private static readonly Dictionary<string, string> NotableInstances = new()
    {
        ["Spirit Vale"] = "Raid",
        ["Salvation Pass"] = "Raid",
        ["Stronghold of the Faithful"] = "Raid",
        ["Bastion of the Penitent"] = "Raid",
        ["Hall of Chains"] = "Raid",
        ["Mythwright Gambit"] = "Raid",
        ["The Key of Ahdashim"] = "Raid",
        ["Mount Balrior"] = "Raid",
        ["Gilded Hollow"] = "Guild hall",
        ["Lost Precipice"] = "Guild hall",
        ["Windswept Haven"] = "Guild hall",
        ["Isle of Reflection"] = "Guild hall",
        ["Hearth's Glow"] = "Guild hall",
        ["Comosus Isle"] = "Guild hall",
    };

    /// <summary>
    /// From the Maps table: the ids of the maps whose landmarks count — the explorable ones, plus the raids and guild
    /// halls, which are instances (null while the worker hasn't synced the table) — and the raids and guild halls to
    /// label, one map each.
    /// </summary>
    private Task<(HashSet<int>? Explorable, List<NotableMap> Notable)> ReadMapCatalogAsync() =>
        Task.Run(() =>
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
                List<int> ids = db.Maps.AsNoTracking().Where(m => m.Type == "Public").Select(m => m.Id).ToList();
                string[] names = NotableInstances.Keys.ToArray();
                // A map only on negative floors (Hall of Chains, Mythwright Gambit) sits in a space of its own, whose
                // coordinates don't line up with the continent's.
                var notableMaps = db
                    .Maps.AsNoTracking()
                    .Where(m => names.Contains(m.Name))
                    .OrderBy(m => m.Id)
                    .AsEnumerable()
                    .Where(m => m.Floors.Any(floor => floor >= 0))
                    .ToList();
                ids.AddRange(notableMaps.Select(m => m.Id));
                List<NotableMap> notable = notableMaps
                    .DistinctBy(m => m.Name)
                    .Select(m => new NotableMap(
                        m.Id,
                        m.Name,
                        NotableInstances[m.Name],
                        m.ContinentRect.Min.X,
                        m.ContinentRect.Min.Y,
                        m.ContinentRect.Max.X,
                        m.ContinentRect.Max.Y
                    ))
                    .ToList();
                return (ids.Count > 0 ? ids.ToHashSet() : null, notable);
            }
            catch (Exception)
            {
                return ((HashSet<int>?)null, new List<NotableMap>());
            }
        });

    private static async Task<MapLandmarks?> ReadAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<MapLandmarks>(stream, FileOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task WriteAsync(string path, MapLandmarks landmarks)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".tmp";
            await using (FileStream stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, landmarks, FileOptions);
            }

            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception)
        {
            // Not cached this time; fetched again next time.
        }
    }

    /// <summary>A raid or guild hall map, from the Maps table.</summary>
    /// <param name="Kind">"Raid" or "Guild hall", written under its name.</param>
    internal sealed record NotableMap(
        int Id,
        string Name,
        string Kind,
        double MinX,
        double MinY,
        double MaxX,
        double MaxY
    )
    {
        public double CenterX => (MinX + MaxX) / 2;
        public double CenterY => (MinY + MaxY) / 2;
        public double Area => (MaxX - MinX) * (MaxY - MinY);
    }
}
