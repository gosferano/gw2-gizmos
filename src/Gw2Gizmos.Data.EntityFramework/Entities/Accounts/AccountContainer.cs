namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// Where a synced item is held — the location value stored (as a string) on <see cref="AccountItemObservation"/>
/// and the slot grids. Plain string constants rather than an enum, so the column is human-readable in the
/// database and adding a location never shifts a persisted integer ordinal. <see cref="AccountContainerSlot"/>
/// only uses the slot-based stores (<see cref="Bank"/> / <see cref="SharedInventory"/>); character bags get their
/// own per-character slot snapshot.
/// </summary>
public static class AccountContainer
{
    public const string Bank = "Bank";
    public const string SharedInventory = "SharedInventory";
    public const string MaterialStorage = "MaterialStorage";
    public const string CharacterInventory = "CharacterInventory";
}
