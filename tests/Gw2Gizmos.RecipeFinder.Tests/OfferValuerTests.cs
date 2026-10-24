using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.RecipeFinder.Tests;

public class OfferValuerTests
{
    private static readonly Dictionary<string, decimal> Weights = new()
    {
        ["Karma"] = 0.2m,
        ["Tale of Dungeon Delving"] = 3.8m,
    };

    private static int ItemValue(int id) => id == 19721 ? 200 : 0; // Glob of Ectoplasm ~ 200c

    [Fact]
    public void CoinValue_SumsEveryComponentByKind()
    {
        // 1050 Karma + 25 ecto = 1050*0.2 + 25*200 = 210 + 5000.
        var offer = new VendorOffer(1,
        [
            new VendorCost(1050, "Karma", null, "karma-icon", IsCoin: false),
            new VendorCost(25, "Glob of Ectoplasm", 19721, null, IsCoin: false),
        ]);

        Assert.Equal(5210m, OfferValuer.CoinValue(offer, Weights, ItemValue));
    }

    [Fact]
    public void CoinValue_CountsCoinOneToOne()
    {
        var offer = new VendorOffer(1, [new VendorCost(480, "Coin", null, null, IsCoin: true)]);
        Assert.Equal(480m, OfferValuer.CoinValue(offer, Weights, ItemValue));
    }

    [Fact]
    public void CoinValue_IsNull_WhenAnAccountCurrencyHasNoWeight()
    {
        var offer = new VendorOffer(1, [new VendorCost(5, "WvW Skirmish Claim Ticket", null, "icon", IsCoin: false)]);
        Assert.Null(OfferValuer.CoinValue(offer, Weights, ItemValue));
    }

    [Fact]
    public void CoinValue_IsNull_ForABoundTokenCurrencyWithNoTradingPostPrice_EvenAlongsideCoin()
    {
        // Found Belonging (item 50030) is an account-bound token used as a currency: it has an item id but no
        // trading-post price (itemValue → 0). Pairing it with 5 gold must NOT price the offer at just the 5 gold.
        var offer = new VendorOffer(1,
        [
            new VendorCost(12_500, "Found Belonging", 50030, "icon", IsCoin: false),
            new VendorCost(50_000, "Coin", null, null, IsCoin: true),
        ]);

        Assert.Null(OfferValuer.CoinValue(offer, Weights, ItemValue));
    }

    [Fact]
    public void CoinValue_IsNull_ForUnvaluedAccountCurrency_EvenWithASpuriousSameNamedItem()
    {
        // Festival Token is an account currency (CurrencyId set) with no weight, but it also has an untradeable
        // same-named item (id 66224, value 0). It must NOT be priced at that item's 0 — that wrongly made the
        // whole offer "free" and win the cheapest ranking.
        var offer = new VendorOffer(1,
            [new VendorCost(25_000, "Festival Token", 66224, "icon", IsCoin: false, CurrencyId: 50)]);

        Assert.Null(OfferValuer.CoinValue(offer, Weights, ItemValue));
    }

    [Fact]
    public void CoinOnlyValue_CountsCoinAndTradeableItems_AndAccountCurrenciesAsNothing()
    {
        // INFUZ-5959's Shard of Crystallized Mists Essence: 2g 88s and 300 Fractal Relics; here with 2 ecto too.
        var offer = new VendorOffer(1,
        [
            new VendorCost(28_800, "Coin", null, null, IsCoin: true),
            new VendorCost(300, "Fractal Relic", null, "icon", IsCoin: false, CurrencyId: 7),
            new VendorCost(2, "Glob of Ectoplasm", 19721, null, IsCoin: false),
            new VendorCost(1050, "Karma", null, "karma-icon", IsCoin: false, CurrencyId: 2),
        ]);

        Assert.Equal(29_200m, OfferValuer.CoinOnlyValue(offer, ItemValue));
    }

    [Fact]
    public void CoinOnlyValue_IsNull_WithAPartThatIsNeitherAnItemNorACurrency()
    {
        // A League Vendor's Mystic Clover lists "2 Ectoplasm", resolved to no item: it isn't free.
        var offer = new VendorOffer(1,
        [
            new VendorCost(2, "Mystic Coin", 19976, null, IsCoin: false),
            new VendorCost(2, "Ectoplasm", null, null, IsCoin: false),
        ]);

        Assert.Null(OfferValuer.CoinOnlyValue(offer, ItemValue));
    }

    [Fact]
    public void CoinOnlyValue_IsNull_ForAnOfferCostingNoMoney()
    {
        // The Wizard's Vault's Mystic Clover, for 60 Astral Acclaim: it mustn't make the clover free.
        var offer = new VendorOffer(1,
            [new VendorCost(60, "Astral Acclaim", null, "icon", IsCoin: false, CurrencyId: 63)]);

        Assert.Null(OfferValuer.CoinOnlyValue(offer, ItemValue));
    }
}
