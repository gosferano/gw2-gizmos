using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework;

/// <summary>
/// Values what the account has unlocked: each skin, dye, mini, novelty, outfit, finisher, mail carrier, glider, jade
/// bot and learned recipe at the cheapest item that unlocks it and has a value
/// (<see cref="AccountHoldingsReconstructor.ItemValues"/>, with coin costs). Skins are unlocked by the items that have
/// them as their default skin, and by the consumables that unlock them; dyes by their dye items; minis by their item;
/// recipes by their recipe sheets; the rest by the items their catalogs list (<see cref="UnlockItem"/>). An unlock no
/// item with a value grants (gem store, achievement rewards, recipes learned by discovery) counts as nothing, and so
/// does one whose item the account holds, has equipped or keeps in its legendary armory, as that item is already
/// counted (gw2efficiency's figure without "items in inventories").
/// </summary>
public static class AccountUnlockValuation
{
    /// <summary>The kind for learned recipes, beside <see cref="AccountUnlockKind"/>'s.</summary>
    public const string Recipe = "Recipe";

    /// <summary>What each kind of the account's unlocks is worth, in copper, by kind.</summary>
    public static Dictionary<string, long> ByKind(Gw2GizmosDbContext db, string accountId)
    {
        Dictionary<string, Dictionary<int, List<int>>> unlocks = ItemsByUnlock(db, accountId);
        HashSet<int> held = HeldItems(db, accountId);
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(
            db, unlocks.Values.SelectMany(u => u.Values).SelectMany(items => items).Distinct().ToList(),
            coinCosts: true);
        return unlocks.ToDictionary(
            kind => kind.Key,
            kind => kind.Value.Values.Where(items => !items.Any(held.Contains)).Sum(items => items
                .Select(itemId => values.GetValueOrDefault(itemId))
                .Where(value => value > 0)
                .DefaultIfEmpty(0)
                .Min()));
    }

    /// <summary>Every item that unlocks something the account has (for working out their coin costs).</summary>
    public static IReadOnlyCollection<int> UnlockingItems(Gw2GizmosDbContext db, string accountId) =>
        ItemsByUnlock(db, accountId).Values.SelectMany(u => u.Values).SelectMany(items => items).Distinct().ToList();

    // What the account holds in its bank, storage and inventories, has equipped or keeps in its legendary armory.
    private static HashSet<int> HeldItems(Gw2GizmosDbContext db, string accountId)
    {
        HashSet<int> held = AccountHoldingsReconstructor.ItemTotalsAsOf(db, accountId, DateTimeOffset.MaxValue).Keys
            .ToHashSet();
        held.UnionWith(db.CharacterEquippedItems.AsNoTracking()
            .Where(e => e.AccountId == accountId)
            .Select(e => e.ItemId));
        held.UnionWith(db.AccountLegendaryArmory.AsNoTracking()
            .Where(l => l.AccountId == accountId)
            .Select(l => l.ItemId));
        return held;
    }

    // For each kind, each unlock the account has, with the items that grant it (none for some).
    private static Dictionary<string, Dictionary<int, List<int>>> ItemsByUnlock(
        Gw2GizmosDbContext db, string accountId)
    {
        Dictionary<string, HashSet<int>> unlocked = db.AccountUnlocks.AsNoTracking()
            .Where(u => u.AccountId == accountId)
            .Select(u => new { u.Kind, u.UnlockId })
            .AsEnumerable()
            .GroupBy(u => u.Kind)
            .ToDictionary(g => g.Key, g => g.Select(u => u.UnlockId).ToHashSet());
        unlocked[Recipe] = db.AccountRecipes.AsNoTracking()
            .Where(r => r.AccountId == accountId)
            .Select(r => r.RecipeId)
            .ToHashSet();
        HashSet<int> Of(string kind) => unlocked.GetValueOrDefault(kind) ?? [];

        var grants = new List<(string Kind, int UnlockId, int ItemId)>();
        HashSet<int> skins = Of(AccountUnlockKind.Skin);
        grants.AddRange(db.Items.AsNoTracking()
            .Where(i => i.DefaultSkin != null && skins.Contains(i.DefaultSkin.Value))
            .Select(i => new { Skin = i.DefaultSkin!.Value, i.Id })
            .AsEnumerable()
            .Select(i => (AccountUnlockKind.Skin, i.Skin, i.Id)));
        grants.AddRange(db.Set<ConsumableSkin>().AsNoTracking()
            .Where(s => skins.Contains(s.SkinId))
            .Select(s => new { s.SkinId, s.ConsumableId })
            .AsEnumerable()
            .Select(s => (AccountUnlockKind.Skin, s.SkinId, s.ConsumableId)));

        HashSet<int> dyes = Of(AccountUnlockKind.Dye);
        grants.AddRange(db.ConsumableDetails.AsNoTracking()
            .Where(c => c.UnlockType == "Dye" && c.ColorId != null && dyes.Contains(c.ColorId.Value))
            .Select(c => new { Color = c.ColorId!.Value, c.ItemId })
            .AsEnumerable()
            .Select(c => (AccountUnlockKind.Dye, c.Color, c.ItemId)));

        HashSet<int> minis = Of(AccountUnlockKind.Mini);
        grants.AddRange(db.Minis.AsNoTracking()
            .Where(m => minis.Contains(m.Id) && m.ItemId > 0)
            .Select(m => new { m.Id, m.ItemId })
            .AsEnumerable()
            .Select(m => (AccountUnlockKind.Mini, m.Id, m.ItemId)));

        HashSet<int> recipes = Of(Recipe);
        grants.AddRange(db.ConsumableDetails.AsNoTracking()
            .Where(c => c.RecipeId != null && recipes.Contains(c.RecipeId.Value))
            .Select(c => new { Recipe = c.RecipeId!.Value, c.ItemId })
            .AsEnumerable()
            .Select(c => (Recipe, c.Recipe, c.ItemId)));
        grants.AddRange(db.Set<ConsumableExtraRecipe>().AsNoTracking()
            .Where(r => recipes.Contains(r.RecipeId))
            .Select(r => new { r.RecipeId, r.ConsumableId })
            .AsEnumerable()
            .Select(r => (Recipe, r.RecipeId, r.ConsumableId)));

        foreach (IGrouping<string, UnlockItem> kind in db.UnlockItems.AsNoTracking().AsEnumerable()
                     .GroupBy(u => u.Kind))
        {
            HashSet<int> ofKind = Of(kind.Key);
            grants.AddRange(kind.Where(u => ofKind.Contains(u.UnlockId)).Select(u => (u.Kind, u.UnlockId, u.ItemId)));
        }

        Dictionary<(string Kind, int UnlockId), List<int>> itemsOf = grants
            .GroupBy(g => (g.Kind, g.UnlockId))
            .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).Distinct().ToList());
        return unlocked.ToDictionary(
            kind => kind.Key,
            kind => kind.Value.ToDictionary(id => id, id => itemsOf.GetValueOrDefault((kind.Key, id)) ?? []));
    }
}
