namespace Gw2Gizmos.RecipeFinder.Cli;

public interface IPriceService
{
    Task<(decimal BuyPrice, decimal SellPrice)> GetPricesAsync(int itemId, CancellationToken ct);
}
