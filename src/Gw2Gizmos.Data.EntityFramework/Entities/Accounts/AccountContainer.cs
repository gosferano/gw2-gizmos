namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// Where a synced item is held. Used as the location discriminator on the unified <see cref="AccountItemObservation"/>
/// log; <see cref="AccountContainerSlot"/> only ever uses the slot-based stores (<see cref="Bank"/> /
/// <see cref="SharedInventory"/>), and character bags get their own per-character slot snapshot.
/// </summary>
public enum AccountContainer
{
    Bank,
    SharedInventory,
    MaterialStorage,
    CharacterInventory,
}
