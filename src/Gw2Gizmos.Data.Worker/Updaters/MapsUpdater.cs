using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Maps;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Api = Gw2Gizmos.Gw2Api.Contract.V2.Maps;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for map master data from <c>/v2/maps</c> (public; no key): about 1,100 maps in one bulk
/// request, changing only with game releases, so it runs daily. Lets the app turn the map id MumbleLink reports into
/// a name and region. Maps the API stops listing are kept, so old sessions can still name where they were played.
/// </summary>
public class MapsUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<MapsUpdater> _logger;

    public MapsUpdater(Gw2GizmosDbContext dbContext, IGw2ApiClientFactory apiClientFactory, ILogger<MapsUpdater> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateMaps(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting maps update...");

        Api.Map[]? maps = await _apiClient.V2.Maps.GetAll(stoppingToken);
        if (maps is null || maps.Length == 0)
        {
            _logger.LogWarning("Maps API returned no data; skipping maps update.");
            return;
        }

        Dictionary<int, Map> existing = await _dbContext.Maps.ToDictionaryAsync(m => m.Id, stoppingToken);
        foreach (Api.Map map in maps)
        {
            if (!existing.TryGetValue(map.Id, out Map? row))
            {
                row = new Map { Id = map.Id };
                _dbContext.Maps.Add(row);
            }

            row.Name = map.Name;
            row.MinLevel = map.MinLevel;
            row.MaxLevel = map.MaxLevel;
            row.Type = map.Type.Value;
            row.DefaultFloor = map.DefaultFloor;
            row.Floors = map.Floors;
            row.RegionId = map.RegionId;
            row.RegionName = map.RegionName;
            row.ContinentId = map.ContinentId;
            row.ContinentName = map.ContinentName;
            row.MapRect = ToRect(map.MapRect);
            row.ContinentRect = ToRect(map.ContinentRect);
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Maps update completed: {Count} maps.", maps.Length);
    }

    /// <summary>The API's <c>[[x1, y1], [x2, y2]]</c> corner pair as a <see cref="MapRect"/> (zeros if malformed).</summary>
    private static MapRect ToRect(int[][] corners) =>
        corners is [[var x1, var y1, ..], [var x2, var y2, ..], ..]
            ? new MapRect { Min = new MapPoint { X = x1, Y = y1 }, Max = new MapPoint { X = x2, Y = y2 } }
            : new MapRect();
}
