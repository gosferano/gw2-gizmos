using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.RecipeFinder;

public class Configuration
{
    public PriceType BuyPriceType { get; set; } = PriceType.SellOrder;
    public PriceType SellPriceType { get; set; } = PriceType.BuyOrder;

    /// <summary>
    /// Whether account currencies (karma, Fractal Relics, …) count, at the coin weight derived for each. Without it
    /// only coin does: a vendor offer costs its coin and any tradeable items it takes, its account currencies
    /// nothing, so an item only sold for karma costs nothing too.
    /// </summary>
    public bool CountAccountCurrencies { get; set; } = true;

    /// <summary>Only coin counts: what something costs to buy for coin or make from what coin buys.</summary>
    public static Configuration CoinOnly =>
        new()
        {
            BuyPriceType = PriceType.SellOrder,
            SellPriceType = PriceType.BuyOrder,
            CountAccountCurrencies = false,
        };

    public static Configuration Default =>
        new() { BuyPriceType = PriceType.SellOrder, SellPriceType = PriceType.BuyOrder };
}
