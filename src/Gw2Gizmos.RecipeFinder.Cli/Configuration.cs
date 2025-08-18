using Gw2Gizmos.RecipeFinder.Cli.Model;

namespace Gw2Gizmos.RecipeFinder.Cli;

public class Configuration
{
    public PriceType BuyPriceType { get; set; } = PriceType.SellOrder;
    public PriceType SellPriceType { get; set; } = PriceType.BuyOrder;

    public static Configuration Default =>
        new() { BuyPriceType = PriceType.SellOrder, SellPriceType = PriceType.BuyOrder };
}
