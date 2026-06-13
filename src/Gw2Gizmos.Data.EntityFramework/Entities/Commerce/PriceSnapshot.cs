using System;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Commerce;

/// <summary>
/// One recorded point in an item's trading-post history: its best buy/sell prices, total demand/supply,
/// and the units that traded since the previous point. Append-only — a 5-minute poller writes one row per
/// tradeable item, and a retention pass downsamples it over time (5-min → hourly → daily). Prices and
/// demand/supply take the latest value when points are collapsed; <see cref="Sold"/>/<see cref="Bought"/>
/// are <em>summed</em>, so total traded volume over any range is preserved regardless of resolution.
/// </summary>
[Index(nameof(ItemId), nameof(TimestampUtc))]
public class PriceSnapshot
{
    /// <summary>Surrogate key; long because the table accumulates millions of rows before downsampling.</summary>
    public long Id { get; set; }

    public int ItemId { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>Highest standing buy order (copper) at this time; null when nobody was buying.</summary>
    public int? Buy { get; set; }

    /// <summary>Lowest standing sell listing (copper) at this time; null when nothing was listed.</summary>
    public int? Sell { get; set; }

    /// <summary>Total units wanted across all buy orders (demand) at this time.</summary>
    public int Demand { get; set; }

    /// <summary>Total units listed for sale across all sell orders (supply) at this time.</summary>
    public int Supply { get; set; }

    /// <summary>
    /// Units sold since the previous point, estimated as the drop in <see cref="Supply"/> (units that left
    /// the sell book). Summed when points are downsampled. Like every GW2 volume estimate this also counts
    /// cancelled sell listings and misses trades that complete within a single polling gap.
    /// </summary>
    public int Sold { get; set; }

    /// <summary>Units bought since the previous point, estimated as the drop in <see cref="Demand"/>.</summary>
    public int Bought { get; set; }
}
