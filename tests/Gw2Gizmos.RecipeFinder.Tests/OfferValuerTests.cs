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
}
