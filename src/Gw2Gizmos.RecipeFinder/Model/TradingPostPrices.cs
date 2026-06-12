namespace Gw2Gizmos.RecipeFinder.Model;

public class TradingPostPrices
{
    public int SellOrderPrice { get; set; }
    public int BuyOrderPrice { get; set; }

    public TradingPostPrices(int sellOrderPrice, int buyOrderPrice)
    {
        SellOrderPrice = sellOrderPrice;
        BuyOrderPrice = buyOrderPrice;
    }
}
