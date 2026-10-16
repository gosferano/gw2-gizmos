using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Maps;

/// <summary>
/// A map from <c>/v2/maps</c> (master data, refreshed daily): zones, cities, instances and the like. Resolves the map
/// id MumbleLink reports to a name and region, and its rectangles place MumbleLink positions on the world map.
/// </summary>
[Table("Maps")]
public class Map
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    public int MinLevel { get; set; }

    public int MaxLevel { get; set; }

    /// <summary>The API's map type, e.g. "Public", "Instance", "Center" (a WvW map).</summary>
    [MaxLength(32)]
    public string Type { get; set; } = "";

    public int DefaultFloor { get; set; }

    /// <summary>The continent floors the map appears on (stored as a JSON array).</summary>
    public int[] Floors { get; set; } = [];

    public int? RegionId { get; set; }

    [MaxLength(128)]
    public string? RegionName { get; set; }

    public int? ContinentId { get; set; }

    [MaxLength(128)]
    public string? ContinentName { get; set; }

    /// <summary>The map's bounds in its own coordinates — MumbleLink's player position (stored as JSON).</summary>
    public MapRect MapRect { get; set; } = new();

    /// <summary>Where the map sits on its continent, the world map's coordinates (stored as JSON).</summary>
    public MapRect ContinentRect { get; set; } = new();
}

/// <summary>A rectangle from the API's <c>[[x1, y1], [x2, y2]]</c> pair: its two opposite corners.</summary>
public class MapRect
{
    public MapPoint Min { get; set; } = new();

    public MapPoint Max { get; set; } = new();
}

public class MapPoint
{
    public int X { get; set; }

    public int Y { get; set; }
}
