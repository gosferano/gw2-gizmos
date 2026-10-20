using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Static.Crafting;
using Gw2Gizmos.Desktop.Core.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Opens an item on the Items page from wherever its name is shown, for the items that page lists: tradeable (seen on
/// the trading post) or craftable (any API or static recipe, e.g. the Mystic Forge's). The listed ids are loaded once
/// in the background, like <see cref="ItemRarities"/>; names rendered before they're ready wait for
/// <see cref="Ready"/>. Items that became listed since read as unlisted (plain names) until the next app start.
/// </summary>
public sealed class ItemLinks
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PageTargets _targets;
    private readonly INavigationService _navigation;
    private readonly Lazy<Task<HashSet<int>>> _listed;

    public ItemLinks(IServiceScopeFactory scopeFactory, PageTargets targets, INavigationService navigation)
    {
        _scopeFactory = scopeFactory;
        _targets = targets;
        _navigation = navigation;
        _listed = new Lazy<Task<HashSet<int>>>(() => Task.Run(Load));
    }

    /// <summary>Starts loading the listed items in the background, if not already started.</summary>
    public void StartLoading() => _ = _listed.Value;

    /// <summary>Completes once the listed items are loaded (starting the load if it hasn't started).</summary>
    public Task Ready => _listed.Value;

    /// <summary>Whether the Items page lists the item; false when not loaded yet (await <see cref="Ready"/>).</summary>
    public bool IsListed(int itemId) =>
        _listed.Value is { IsCompletedSuccessfully: true } loaded && loaded.Result.Contains(itemId);

    /// <summary>Opens the Items page with the item selected, as the global search does.</summary>
    public void Open(int itemId)
    {
        _targets.OpenItem(itemId);
        _navigation.NavigateTo(AppPage.Items, reload: true);
    }

    /// <summary>
    /// The items the Items page lists, each with whether it's tradeable (true) or only craftable (false). The Items
    /// page's own rule, shared with the global search.
    /// </summary>
    public static Dictionary<int, bool> ReadListed(Gw2GizmosDbContext db)
    {
        var listed = new Dictionary<int, bool>();
        foreach (int id in db.Recipes.AsNoTracking().Select(r => r.OutputItemId).Distinct())
        {
            listed[id] = false;
        }

        foreach (int id in StaticRecipes.ByOutputItemId.Keys)
        {
            listed[id] = false;
        }

        foreach (int id in db.PriceSnapshots.AsNoTracking().Select(s => s.ItemId).Distinct())
        {
            listed[id] = true;
        }

        return listed;
    }

    private HashSet<int> Load()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            // Only items the catalog has: the Items page lists catalog items, so others would open nothing.
            HashSet<int> listed = ReadListed(db).Keys.ToHashSet();
            listed.IntersectWith(db.Items.AsNoTracking().Select(i => i.Id));
            return listed;
        }
        catch (Exception)
        {
            // Database not ready yet (fresh install): names stay plain.
            return [];
        }
    }
}
