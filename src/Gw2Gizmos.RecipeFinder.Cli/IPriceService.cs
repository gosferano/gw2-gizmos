namespace Gw2Gizmos.RecipeFinder.Cli;

public interface IPriceService
{
    Task<TradingPostPrices> GetPricesAsync(int itemId, CancellationToken ct);
}
