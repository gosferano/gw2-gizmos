using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Map names by map id, from the Maps table the worker syncs from <c>/v2/maps</c>. Found names are cached for the
/// run; a miss (the first sync hasn't landed yet, or an unknown id) isn't, so asking again picks the name up later.
/// </summary>
public sealed class MapNames
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<int, string> _names = new();

    public MapNames(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>The map's name, or null when it isn't in the database (yet).</summary>
    public async Task<string?> GetAsync(int mapId)
    {
        if (mapId <= 0)
        {
            return null;
        }

        if (_names.TryGetValue(mapId, out string? cached))
        {
            return cached;
        }

        string? name = await Task.Run(() =>
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
                return db.Maps.AsNoTracking().Where(m => m.Id == mapId).Select(m => m.Name).FirstOrDefault();
            }
            catch (Exception)
            {
                // Database not ready (e.g. before the worker's migration): treat as not found.
                return null;
            }
        });

        if (name is not null)
        {
            _names[mapId] = name;
        }

        return name;
    }
}
