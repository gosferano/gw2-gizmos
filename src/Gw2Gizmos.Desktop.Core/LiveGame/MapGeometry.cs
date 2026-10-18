using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A map's rectangle in its own coordinates (inches, Y pointing north, as MumbleLink's avatar position) and on its
/// continent (Y pointing south), from <c>/v2/maps</c>.
/// </summary>
public sealed record MapFrames(
    double MapMinX,
    double MapMinY,
    double MapMaxX,
    double MapMaxY,
    double ContinentMinX,
    double ContinentMinY,
    double ContinentMaxX,
    double ContinentMaxY
)
{
    /// <summary>MumbleLink's avatar position is in metres; the maps' own coordinates are in inches.</summary>
    private const double InchesPerMetre = 39.3701;

    /// <summary>
    /// The continent point of an avatar position in metres: across (<paramref name="x"/>) and north
    /// (<paramref name="north"/>, MumbleLink's Z), scaled from the map's rectangle onto its continent rectangle.
    /// </summary>
    public (double X, double Y) ToContinent(double x, double north)
    {
        double mapX = x * InchesPerMetre;
        double mapY = north * InchesPerMetre;
        double continentX = ContinentMinX + (mapX - MapMinX) / (MapMaxX - MapMinX) * (ContinentMaxX - ContinentMinX);
        double continentY = ContinentMinY + (MapMaxY - mapY) / (MapMaxY - MapMinY) * (ContinentMaxY - ContinentMinY);
        return (continentX, continentY);
    }
}

/// <summary>
/// The maps' rectangles by map id, from the Maps table the worker syncs, read once per map and kept. A map not
/// looked up yet starts loading and reads as unknown until it's in.
/// </summary>
public sealed class MapGeometry
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<int, MapFrames?> _frames = new();
    private readonly ConcurrentDictionary<int, byte> _loading = new();

    public MapGeometry(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>The map's rectangles if they've been read; otherwise starts reading them and returns null.</summary>
    public MapFrames? Get(int mapId)
    {
        if (_frames.TryGetValue(mapId, out MapFrames? frames))
        {
            return frames;
        }

        if (_loading.TryAdd(mapId, 0))
        {
            _ = LoadAsync(mapId);
        }

        return null;
    }

    private async Task LoadAsync(int mapId)
    {
        try
        {
            MapFrames? frames = await Task.Run(() =>
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
                var map = db.Maps.AsNoTracking().FirstOrDefault(m => m.Id == mapId);
                return map is null
                    ? null
                    : new MapFrames(
                        map.MapRect.Min.X,
                        map.MapRect.Min.Y,
                        map.MapRect.Max.X,
                        map.MapRect.Max.Y,
                        map.ContinentRect.Min.X,
                        map.ContinentRect.Min.Y,
                        map.ContinentRect.Max.X,
                        map.ContinentRect.Max.Y
                    );
            });
            // A map the worker hasn't synced yet isn't remembered as missing: it's asked for again next time.
            if (frames is not null)
            {
                _frames[mapId] = frames;
            }
        }
        catch (Exception)
        {
            // The worker may be mid-write; asked for again next time.
        }
        finally
        {
            _loading.TryRemove(mapId, out _);
        }
    }
}
