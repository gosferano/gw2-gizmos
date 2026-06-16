using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo;

namespace Gw2Gizmos.Data.Worker.Features;

/// <summary>
/// A toggle-able unit of worker work that needs specific GW2 API-key permissions. Each feature is gated
/// independently (see <see cref="Configuration.IFeatureGate"/>) and the worker warns when an enabled feature's
/// key is missing a required permission. The catalog lives in the worker because these are worker features; the
/// desktop (which references this assembly) reuses it for the Settings toggles and the API Keys permission view.
/// </summary>
/// <param name="Key">Stable identifier — also the <c>Worker:Features:&lt;Key&gt;</c> config/toggle key.</param>
/// <param name="Display">Human label for the Settings page.</param>
/// <param name="Description">One-line explanation of what the feature syncs.</param>
/// <param name="RequiredPermissions">GW2 permissions the feature needs (lowercase wire strings).</param>
public sealed record WorkerFeature(
    string Key,
    string Display,
    string Description,
    IReadOnlyList<string> RequiredPermissions
);

/// <summary>The catalog of worker features and the permission helpers shared by the gate, the warning, and the UI.</summary>
public static class WorkerFeatures
{
    // Permission names come from TokenPermission (the single source). Public-data features (items, prices) need
    // no key, so their permission list is empty; every account feature needs the account permission, so the
    // per-feature lists include it and a single MissingPermissions check covers it.
    public static readonly WorkerFeature ItemsSync = new(
        "ItemsSync", "Item data", "The item catalog — names, icons and details.",
        Array.Empty<string>());

    public static readonly WorkerFeature RecipesSync = new(
        "RecipesSync", "Recipes", "Crafting recipes used by the recipe tree.",
        Array.Empty<string>());

    public static readonly WorkerFeature PricesSync = new(
        "PricesSync", "Price history", "Trading-post price snapshots; uses more storage.",
        Array.Empty<string>());

    public static readonly WorkerFeature Wallet = new(
        "Wallet", "Wallet", "Currencies and coin balances.",
        new[] { Perm(TokenPermission.Account), Perm(TokenPermission.Wallet) });

    public static readonly WorkerFeature MaterialStorage = new(
        "MaterialStorage", "Material storage", "Crafting materials by category.",
        new[] { Perm(TokenPermission.Account), Perm(TokenPermission.Inventories) });

    public static readonly WorkerFeature Bank = new(
        "Bank", "Bank", "Account bank contents.",
        new[] { Perm(TokenPermission.Account), Perm(TokenPermission.Inventories) });

    public static readonly WorkerFeature SharedInventory = new(
        "SharedInventory", "Shared inventory", "Account-wide shared slots.",
        new[] { Perm(TokenPermission.Account), Perm(TokenPermission.Inventories) });

    public static readonly WorkerFeature CharacterInventory = new(
        "CharacterInventory", "Characters", "Character details and bag contents (one request per character).",
        new[] { Perm(TokenPermission.Account), Perm(TokenPermission.Characters), Perm(TokenPermission.Inventories) });

    public static readonly WorkerFeature PlaySessions = new(
        "PlaySessions", "Play sessions",
        "Tracks play sessions and per-character switches from MumbleLink (Windows only); loot deltas use the "
            + "account features above.",
        new[] { Perm(TokenPermission.Account) });

    /// <summary>All worker features, in display order (public data first, then account-bound).</summary>
    public static IReadOnlyList<WorkerFeature> All { get; } =
        new[] { ItemsSync, RecipesSync, PricesSync, Wallet, MaterialStorage, Bank, SharedInventory, CharacterInventory, PlaySessions };

    /// <summary>
    /// Every GW2 permission that can appear on a key — shown in full on the API Keys page. Sourced from
    /// <see cref="TokenPermission.All"/>, so the names live in exactly one place.
    /// </summary>
    public static IReadOnlyList<string> AllPermissions { get; } =
        TokenPermission.All.Select(Perm).ToArray();

    /// <summary>The permissions a single feature needs, or empty for an unknown key.</summary>
    public static IReadOnlyList<string> Required(string featureKey) =>
        All.FirstOrDefault(f => f.Key == featureKey)?.RequiredPermissions ?? Array.Empty<string>();

    /// <summary>The distinct permissions required by any of the given enabled features.</summary>
    public static IReadOnlyList<string> RequiredPermissions(IEnumerable<string> enabledFeatureKeys) =>
        enabledFeatureKeys.SelectMany(Required).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>
    /// The required permissions (across the enabled features) that <paramref name="keyPermissions"/> doesn't
    /// hold. Drives the worker warning, the API Keys red highlighting, and the dashboard's missing-permission badges.
    /// </summary>
    public static IReadOnlyList<string> MissingPermissions(
        IEnumerable<string> keyPermissions,
        IEnumerable<string> enabledFeatureKeys
    )
    {
        var have = new HashSet<string>(keyPermissions, StringComparer.OrdinalIgnoreCase);
        return RequiredPermissions(enabledFeatureKeys).Where(permission => !have.Contains(permission)).ToArray();
    }

    private static string Perm(TokenPermission permission) => permission.Value;
}
