using System;
using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Data.Worker.Features;

/// <summary>
/// A worker sync loop whose interval the user can tune. <see cref="Key"/> is a stable id — also the
/// <c>Worker:Intervals:&lt;Key&gt;</c> config key (standalone) and the desktop's interval-settings key — and
/// reuses the matching <see cref="WorkerFeature.Key"/> where a sync lines up 1:1 with a feature.
/// </summary>
/// <param name="Key">Stable id / config key.</param>
/// <param name="Display">Human label for the Advanced settings row.</param>
/// <param name="Default">Default cadence (one of the offered presets).</param>
public sealed record WorkerSync(string Key, string Display, TimeSpan Default);

/// <summary>The worker syncs whose interval is user-configurable (on Settings → Advanced). Account's four
/// sections share one pass, so there's a single Account interval. Internal craft-cost / retention loops stay
/// fixed and aren't listed here.</summary>
public static class WorkerSyncs
{
    public static readonly WorkerSync Items = new(WorkerFeatures.ItemsSync.Key, "Item data", TimeSpan.FromDays(1));
    public static readonly WorkerSync Recipes = new(WorkerFeatures.RecipesSync.Key, "Recipes", TimeSpan.FromDays(1));
    public static readonly WorkerSync Prices = new(WorkerFeatures.PricesSync.Key, "Price history", TimeSpan.FromMinutes(5));
    public static readonly WorkerSync Account = new("AccountSync", "Account data", TimeSpan.FromMinutes(5));
    public static readonly WorkerSync Currencies = new("CurrenciesSync", "Currencies", TimeSpan.FromDays(1));
    public static readonly WorkerSync MaterialCategories = new("MaterialCategoriesSync", "Material categories", TimeSpan.FromDays(1));

    /// <summary>All tunable syncs, in display order.</summary>
    public static IReadOnlyList<WorkerSync> All { get; } =
        new[] { Items, Recipes, Prices, Account, Currencies, MaterialCategories };

    /// <summary>The shipped default cadence for a sync, or an hour for an unknown key.</summary>
    public static TimeSpan DefaultInterval(string key) =>
        All.FirstOrDefault(sync => sync.Key == key)?.Default ?? TimeSpan.FromHours(1);

    /// <summary>
    /// The sync that enabling a feature should run immediately: the sync sharing the feature's key
    /// (Items / Recipes / Prices), or the shared <see cref="Account"/> sync for the per-section account features
    /// (wallet / materials / bank / shared inventory), which all ride one account pass.
    /// </summary>
    public static string TriggeredByFeature(string featureKey) =>
        All.FirstOrDefault(sync => sync.Key == featureKey)?.Key ?? Account.Key;
}
