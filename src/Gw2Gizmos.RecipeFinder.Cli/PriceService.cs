using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.RecipeFinder.Cli;

public class PriceService : IPriceService
{
    private readonly Gw2GizmosDbContext _dbContext;

    public PriceService(Gw2GizmosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TradingPostPrices> GetPricesAsync(int itemId, CancellationToken ct)
    {
        int sellPrice = await _dbContext
            .SellListings.Where(sl => sl.CommerceItemListingId == itemId)
            .OrderBy(sl => sl.UnitPrice)
            .Select(sl => sl.UnitPrice)
            .FirstOrDefaultAsync(ct);

        int buyPrice = await _dbContext
            .BuyListings.Where(bl => bl.CommerceItemListingId == itemId)
            .OrderByDescending(bl => bl.UnitPrice)
            .Select(bl => bl.UnitPrice)
            .FirstOrDefaultAsync(ct);

        return new TradingPostPrices(sellPrice > 0 ? sellPrice : 0, buyPrice > 0 ? buyPrice : 0);
    }
}
