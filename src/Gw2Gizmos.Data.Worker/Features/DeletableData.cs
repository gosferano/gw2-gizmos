using System.Collections.Generic;

namespace Gw2Gizmos.Data.Worker.Features;

/// <summary>
/// A category of locally-stored data the user can delete from the Settings → Stored data section. Account-scoped
/// types are deleted for one account; global types (e.g. price history) aren't tied to an account. Most of this
/// data is an append-on-change snapshot of current state, so deleting it clears the stored <i>history</i> — the
/// current values re-sync on the next worker pass (sessions and price history are the exceptions: those are
/// genuinely historical and don't come back).
/// </summary>
/// <param name="Key">Stable identifier carried on the delete request and switched on by the worker's deleter.</param>
/// <param name="Display">Human label for the Settings UI.</param>
/// <param name="AccountScoped">True when the delete applies to a specific account; false for global data.</param>
public sealed record DeletableDataType(string Key, string Display, bool AccountScoped);

/// <summary>The catalog of deletable data categories, shared by the desktop UI and the worker's deleter.</summary>
public static class DeletableData
{
    // Account-scoped.
    public const string Wallet = "Wallet";
    public const string Materials = "Materials";
    public const string Bank = "Bank";
    public const string SharedInventory = "SharedInventory";
    public const string Characters = "Characters";
    public const string Sessions = "Sessions";

    // Targeted account-scoped deletes that act on a single row by its id (DeleteRequest.TargetId), raised from the
    // sessions UI rather than the Settings → Stored data bulk list, so they're deliberately not in AccountTypes.
    public const string Session = "Session";
    public const string SessionSegment = "SessionSegment";

    /// <summary>Every account-scoped category at once (plus the account identity row).</summary>
    public const string AllForAccount = "AllForAccount";

    // Global (not tied to an account).
    public const string PriceHistory = "PriceHistory";

    /// <summary>The per-account categories, in display order (not including <see cref="AllForAccount"/>).</summary>
    public static IReadOnlyList<DeletableDataType> AccountTypes { get; } = new[]
    {
        new DeletableDataType(Wallet, "Wallet", true),
        new DeletableDataType(Materials, "Material storage", true),
        new DeletableDataType(Bank, "Bank", true),
        new DeletableDataType(SharedInventory, "Shared inventory", true),
        new DeletableDataType(Characters, "Characters", true),
        new DeletableDataType(Sessions, "Play sessions", true),
    };

    /// <summary>The global (non-account) categories.</summary>
    public static IReadOnlyList<DeletableDataType> GlobalTypes { get; } = new[]
    {
        new DeletableDataType(PriceHistory, "Price history", false),
    };
}
