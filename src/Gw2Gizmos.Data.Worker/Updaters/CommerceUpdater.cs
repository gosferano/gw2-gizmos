using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;
using Microsoft.EntityFrameworkCore;
using CommerceListing = Gw2Gizmos.Data.EntityFramework.Entities.Commerce.CommerceListing;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Refreshes trading-post buy/sell listings from <c>/v2/commerce/listings</c>. Detects ids
/// present on the market but missing from the items table and signals them on the
/// <see cref="ItemMissingDto"/> channel; upserts listings for items that already exist.
/// </summary>
public class CommerceUpdater
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommerceUpdater> _logger;
    private readonly Gw2ApiClient _apiClient;
    private readonly ChannelWriter<ItemMissingDto> _itemsMissingWriter;
    private const int PageSize = 200;

    public CommerceUpdater(
        IServiceScopeFactory scopeFactory,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<CommerceUpdater> logger,
        Channel<ItemMissingDto> itemsMissing
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _itemsMissingWriter = itemsMissing.Writer;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateCommerceListings(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting commerce listings update...");

        // Fetch all item IDs with commerce data from the API
        int[]? itemIds = await _apiClient.V2.Commerce.Listings.GetIds(stoppingToken);

        if (itemIds is null || itemIds.Length == 0)
        {
            _logger.LogWarning("Commerce listings API returned no ids; skipping commerce listings update.");
            return;
        }

        int[] existingItemIds;
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            existingItemIds = await dbContext.Items.Select(i => i.Id).ToArrayAsync(stoppingToken);
        }

        int[] missingItemIds = itemIds.Except(existingItemIds).ToArray();

        foreach (int missingItemId in missingItemIds)
        {
            await _itemsMissingWriter.WriteAsync(new ItemMissingDto { ItemId = missingItemId }, stoppingToken);
        }

        int[] remainingItemIds = itemIds.Intersect(existingItemIds).ToArray();

        _logger.LogInformation("Total items with commerce data: {Count}", itemIds.Length);

        for (var i = 0; i < remainingItemIds.Length; i += PageSize)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                int[] pageIds = remainingItemIds.Skip(i).Take(PageSize).ToArray();

                _logger.LogInformation(
                    "Processing items {Start} to {End} of {Total}",
                    i + 1,
                    i + pageIds.Length,
                    remainingItemIds.Length
                );

                await UpdateCommerceListingsForItems(pageIds, stoppingToken);

                _logger.LogInformation(
                    "Processed items {Start} to {End}. Total processed: {Total}",
                    i + 1,
                    i + pageIds.Length,
                    i + pageIds.Length
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing items {Start} to {End}", i + 1, i + PageSize);
            }
        }

        _logger.LogInformation("Commerce listings update completed.");
    }

    public async Task UpdateCommerceListingsForItems(IEnumerable<int> itemIds, CancellationToken stoppingToken)
    {
        // Fetch commerce data for the batch of items
        CommerceListings[]? apiListings = await _apiClient.V2.Commerce.Listings.GetByIds(itemIds, stoppingToken);

        if (apiListings is null || apiListings.Length == 0)
        {
            _logger.LogWarning("Commerce listings API returned no data for the requested id batch; they may have been removed.");
            return;
        }

        List<CommerceItemListing> mapped = apiListings.Select(MapToCommerceItemListingEntity).ToList();
        List<int> ids = mapped.Select(m => m.ItemId).ToList();

        // Fresh scope (and DbContext) per page, disposed at the end. That bounds the change tracker
        // to one page's worth of entities — the full-cycle caller pages over the whole market, so a
        // reused context would otherwise accumulate every listing (and its Buys/Sells) for the run.
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        // One existence query for the whole page instead of one per item. Split query because
        // two sibling collections (Buys, Sells) are included — a single query would produce
        // their cartesian product across up to PageSize roots.
        Dictionary<int, CommerceItemListing> existingById = await dbContext
            .CommerceItemListings.Include(l => l.Buys)
            .Include(l => l.Sells)
            .AsSplitQuery()
            .Where(l => ids.Contains(l.ItemId))
            .ToDictionaryAsync(l => l.ItemId, stoppingToken);

        foreach (CommerceItemListing listing in mapped)
        {
            try
            {
                if (existingById.TryGetValue(listing.ItemId, out CommerceItemListing? existing))
                {
                    dbContext.Entry(existing).CurrentValues.SetValues(listing);

                    // Listings are an ordered full snapshot with no stable Id to match on, so
                    // reconcile them in place rather than merging by Id (see ReconcileListings).
                    ReconcileListings(existing.Buys, listing.Buys);
                    ReconcileListings(existing.Sells, listing.Sells);
                }
                else
                {
                    await dbContext.CommerceItemListings.AddAsync(listing, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing commerce data for item ID {ItemId}", listing.ItemId);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private static CommerceItemListing MapToCommerceItemListingEntity(CommerceListings apiListing)
    {
        return new CommerceItemListing
        {
            ItemId = apiListing.Id,
            Buys = apiListing
                .Buys.Select(b => new BuyListing
                {
                    Listings = b.Listings,
                    UnitPrice = b.UnitPrice,
                    Quantity = b.Quantity
                })
                .ToList(),
            Sells = apiListing
                .Sells.Select(s => new SellListing
                {
                    Listings = s.Listings,
                    UnitPrice = s.UnitPrice,
                    Quantity = s.Quantity
                })
                .ToList()
        };
    }

    // Reconciles an existing listing collection to match the incoming snapshot in place:
    // updates the overlap, appends extras, and removes the surplus (orphaned rows are
    // cascade-deleted via the required FK). Scalar fields only are copied so tracked
    // primary keys are never modified. The mapped listings carry no stable Id to match on
    // (incoming Ids are always 0), which is why they are reconciled by position rather than
    // merged by Id — the old Id-based merge never matched and appended every listing on every
    // refresh, growing the tables without bound.
    private static void ReconcileListings<T>(ICollection<T> existing, ICollection<T> incoming)
        where T : CommerceListing
    {
        List<T> existingList = existing.ToList();
        List<T> incomingList = incoming.ToList();
        int common = Math.Min(existingList.Count, incomingList.Count);

        for (var i = 0; i < common; i++)
        {
            existingList[i].Listings = incomingList[i].Listings;
            existingList[i].UnitPrice = incomingList[i].UnitPrice;
            existingList[i].Quantity = incomingList[i].Quantity;
        }

        for (int i = common; i < incomingList.Count; i++)
        {
            existing.Add(incomingList[i]);
        }

        for (int i = common; i < existingList.Count; i++)
        {
            existing.Remove(existingList[i]);
        }
    }
}
