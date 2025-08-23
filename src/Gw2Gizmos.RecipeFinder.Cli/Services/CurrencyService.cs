using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.RecipeFinder.Cli.Services;

public class CurrencyService
{
    private readonly Gw2GizmosDbContext _dbContext;

    public CurrencyService(Gw2GizmosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetCurrencyNameAsync(int currencyId, CancellationToken ct)
    {
        var currency = await _dbContext.Currencies.Where(c => c.Id == currencyId).FirstOrDefaultAsync(ct);
        return currency?.Name ?? $"Unknown Currency ({currencyId})";
    }
}
