using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.Commerce;
using Microsoft.EntityFrameworkCore;
using CommerceListing = Gw2Gizmos.Data.EntityFramework.Entities.Commerce.CommerceListing;

namespace Gw2Gizmos.Data.Worker.Updaters;

public class CommerceUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<CommerceUpdater> _logger;
    private readonly Gw2ApiClient _apiClient;
    private readonly ChannelWriter<ItemMissingDto> _itemsMissingWriter;
    private const int PageSize = 200;

    public CommerceUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<CommerceUpdater> logger,
        Channel<ItemMissingDto> itemsMissing
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _itemsMissingWriter = itemsMissing.Writer;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateCommerceListings(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting commerce listings update...");

        // Fetch all item IDs with commerce data from the API
        int[] itemIds = await _apiClient.V2.Commerce.Listings.GetIds(stoppingToken);
        int[] existingItemIds = await _dbContext.Items.Select(i => i.Id).ToArrayAsync(stoppingToken);
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
        CommerceListings[] apiListings = await _apiClient.V2.Commerce.Listings.GetByIds(itemIds, stoppingToken);

        foreach (CommerceListings apiListing in apiListings)
        {
            try
            {
                // Map API data to entity
                CommerceItemListing commerceItemListing = MapToCommerceItemListingEntity(apiListing);

                // Add or update in the database
                await AddOrUpdateCommerceItemListing(commerceItemListing, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing commerce data for item ID {ItemId}", apiListing.Id);
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
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

    private async Task AddOrUpdateCommerceItemListing(CommerceItemListing listing, CancellationToken ct)
    {
        CommerceItemListing? existing = await _dbContext
            .CommerceItemListings.Include(l => l.Buys)
            .Include(l => l.Sells)
            .FirstOrDefaultAsync(l => l.ItemId == listing.ItemId, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(listing);

            // Update Buys
            foreach (BuyListing buy in listing.Buys)
            {
                CommerceListing? existingBuy = existing.Buys.FirstOrDefault(b => b.Id == buy.Id);
                if (existingBuy != null)
                {
                    _dbContext.Entry(existingBuy).CurrentValues.SetValues(buy);
                }
                else
                {
                    existing.Buys.Add(buy);
                }
            }

            // Update Sells
            foreach (SellListing sell in listing.Sells)
            {
                CommerceListing? existingSell = existing.Sells.FirstOrDefault(s => s.Id == sell.Id);
                if (existingSell != null)
                {
                    _dbContext.Entry(existingSell).CurrentValues.SetValues(sell);
                }
                else
                {
                    existing.Sells.Add(sell);
                }
            }
        }
        else
        {
            await _dbContext.CommerceItemListings.AddAsync(listing, ct);
        }
    }
}
