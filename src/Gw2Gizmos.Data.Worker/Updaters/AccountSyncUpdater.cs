using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Api = Gw2Gizmos.Gw2Api.Contract.V2.Account;
// Aliased so the contract's Character doesn't collide with the Character entity (Entities.Accounts).
using ApiChar = Gw2Gizmos.Gw2Api.Contract.V2.Characters;
using ApiTokenInfo = Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo.TokenInfo;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Syncs the authenticated GW2 account for the current API key. Upserts the <see cref="Account"/> identity, then
/// records item holdings — material storage, bank, shared inventory, and (summed across characters) character bags
/// — as one append-on-change <see cref="AccountItemObservation"/> log discriminated by location, with the wallet
/// in its own log. Slot-based stores also get a replaced current grid: <see cref="AccountContainerSlot"/> for
/// bank/shared and <see cref="CharacterItemSlot"/> per character. Character core details are upserted to the
/// <see cref="Character"/> table from the same full-character fetch. Every row is keyed by account id so multiple
/// accounts coexist.
/// </summary>
public class AccountSyncUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly IGw2ApiClientFactory _apiClientFactory;
    private readonly IGw2ApiKeyProvider _apiKeyProvider;
    private readonly IFeatureGate _featureGate;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountSyncUpdater> _logger;
    private bool _warnedNoKey;

    public AccountSyncUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        IGw2ApiKeyProvider apiKeyProvider,
        IFeatureGate featureGate,
        TimeProvider timeProvider,
        ILogger<AccountSyncUpdater> logger
    )
    {
        _dbContext = dbContext;
        _apiClientFactory = apiClientFactory;
        _apiKeyProvider = apiKeyProvider;
        _featureGate = featureGate;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SyncAccount(CancellationToken stoppingToken)
    {
        IReadOnlyList<string> apiKeys = _apiKeyProvider.GetApiKeys();
        if (apiKeys.Count == 0)
        {
            if (!_warnedNoKey)
            {
                _logger.LogInformation("No GW2 API key available; account sync is idle until one is provided.");
                _warnedNoKey = true;
            }

            return;
        }

        _warnedNoKey = false;

        // Sync every configured key, but each account only once: two keys for the same account would
        // otherwise double-sync it (the desktop blocks that on entry, but the worker is the safety net).
        var seenAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var syncedNames = new List<string>();
        foreach (string apiKey in apiKeys)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                string? name = await SyncOneAccountAsync(apiKey, seenAccounts, stoppingToken);
                if (name is not null)
                {
                    syncedNames.Add(name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Account sync failed for one API key; continuing with the others.");
            }
        }

        _logger.LogInformation(
            "Account sync completed for {Count} account(s): {Accounts}.",
            syncedNames.Count,
            syncedNames.Count == 0 ? "(none)" : string.Join(", ", syncedNames)
        );
    }

    /// <summary>Syncs the account for one key; returns its name, or null when skipped (no data, or a duplicate
    /// account already synced this run).</summary>
    private async Task<string?> SyncOneAccountAsync(
        string apiKey,
        HashSet<string> seenAccounts,
        CancellationToken stoppingToken
    )
    {
        Gw2ApiClient client = _apiClientFactory.Create(apiKey, Locale.English);

        Api.Account? account = await client.V2.Account.GetBlob(stoppingToken);
        if (account is null || string.IsNullOrEmpty(account.Id))
        {
            _logger.LogWarning("Account endpoint returned no data; an API key may lack the 'account' permission.");
            return null;
        }

        if (!seenAccounts.Add(account.Id))
        {
            // Another key already synced this account this run — dedupe.
            return null;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        await UpsertAccountAsync(account, now, stoppingToken);

        // The key's live permissions, so an enabled feature whose key lacks a permission can be warned about
        // up front rather than silently failing the request.
        ApiTokenInfo? token = await client.V2.TokenInfo.GetBlob(stoppingToken);
        IReadOnlyList<string> permissions = token?.Permissions
            .Select(permission => permission.ToString().ToLowerInvariant())
            .ToArray() ?? Array.Empty<string>();

        // Each section is gated by its own feature and is independent: a disabled feature or a missing
        // permission on one endpoint shouldn't affect the others.
        await RunSection(WorkerFeatures.Wallet, account, permissions, () => SyncWalletAsync(client, account.Id, now, stoppingToken));
        await RunSection(WorkerFeatures.MaterialStorage, account, permissions, () => SyncMaterialsAsync(client, account.Id, now, stoppingToken));
        await RunSection(WorkerFeatures.Bank, account, permissions, () => SyncBankAsync(client, account.Id, now, stoppingToken));
        await RunSection(WorkerFeatures.SharedInventory, account, permissions, () => SyncSharedInventoryAsync(client, account.Id, now, stoppingToken));
        await RunSection(WorkerFeatures.CharacterInventory, account, permissions, () => SyncCharactersAsync(client, account.Id, now, stoppingToken));

        return account.Name;
    }

    /// <summary>
    /// Runs one account section if its feature is enabled and the key holds the required permissions; otherwise
    /// skips it (warning when an enabled feature is missing a permission, so the misconfiguration is visible).
    /// </summary>
    private async Task RunSection(WorkerFeature feature, Api.Account account, IReadOnlyList<string> permissions, Func<Task<int>> action)
    {
        if (!_featureGate.IsEnabled(feature.Key))
        {
            return;
        }

        IReadOnlyList<string> missing = WorkerFeatures.MissingPermissions(permissions, new[] { feature.Key });
        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "{Feature} sync is enabled for {Account} but the key is missing permission(s): {Missing}.",
                feature.Display,
                account.Name,
                string.Join(", ", missing)
            );
            return;
        }

        try
        {
            // Start + completion logged per section (like the price poll) so the log shows each sync running; the
            // count is that section's natural unit (currencies, material/item types, characters).
            _logger.LogInformation("Syncing {Feature} for {Account}...", feature.Display, account.Name);
            int count = await action();
            _logger.LogInformation("{Feature} sync completed for {Account} ({Count}).", feature.Display, account.Name, count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Feature} sync failed for {Account} (API error); continuing.", feature.Display, account.Name);
        }
    }

    private async Task UpsertAccountAsync(Api.Account account, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Account? existing = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountId == account.Id, stoppingToken);
        if (existing is null)
        {
            _dbContext.Accounts.Add(
                new Account
                {
                    AccountId = account.Id,
                    Name = account.Name,
                    World = account.World,
                    LastSyncedUtc = now,
                }
            );
        }
        else
        {
            existing.Name = account.Name;
            existing.World = account.World;
            existing.LastSyncedUtc = now;
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task<int> SyncWalletAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountWalletCurrency[]? wallet = await client.V2.Account.Wallet.GetBlob(stoppingToken);
        if (wallet is null)
        {
            return 0;
        }

        IQueryable<long> maxIds = _dbContext
            .AccountWalletObservations.Where(o => o.AccountId == accountId)
            .GroupBy(o => o.CurrencyId)
            .Select(g => g.Max(x => x.Id));
        Dictionary<int, long> latest = await _dbContext
            .AccountWalletObservations.Where(o => maxIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.CurrencyId, o => o.Value, stoppingToken);

        foreach (Api.AccountWalletCurrency currency in wallet)
        {
            if (!latest.TryGetValue(currency.Id, out long previous) || previous != currency.Value)
            {
                _dbContext.AccountWalletObservations.Add(
                    new AccountWalletObservation
                    {
                        AccountId = accountId,
                        CurrencyId = currency.Id,
                        Value = currency.Value,
                        ObservedAtUtc = now,
                    }
                );
            }
        }

        HashSet<int> present = wallet.Select(c => c.Id).ToHashSet();
        foreach ((int currencyId, long value) in latest)
        {
            if (value != 0 && !present.Contains(currencyId))
            {
                _dbContext.AccountWalletObservations.Add(
                    new AccountWalletObservation
                    {
                        AccountId = accountId,
                        CurrencyId = currencyId,
                        Value = 0,
                        ObservedAtUtc = now,
                    }
                );
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        return wallet.Length;
    }

    private async Task<int> SyncMaterialsAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountMaterial[]? materials = await client.V2.Account.Materials.GetBlob(stoppingToken);
        if (materials is null)
        {
            return 0;
        }

        // Material storage is just items in a location: feed the shared item-observation log. Only currently-held
        // materials (count > 0) are totals; a material dropping to 0 is recorded by the helper's zero-out pass.
        // The same item id can be listed under more than one material-storage category, but each row reports the
        // *same* single storage count — so take it once (Max), not summed (which would double-count), and group
        // so the duplicate id doesn't throw.
        Dictionary<int, int> totals = materials
            .Where(material => material.Count > 0)
            .GroupBy(material => material.Id)
            .ToDictionary(group => group.Key, group => group.Max(material => material.Count));
        await SyncItemObservationsAsync(accountId, AccountContainer.MaterialStorage, totals, now, stoppingToken);
        return totals.Count;
    }

    private async Task<int> SyncBankAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountItem?[]? bank = await client.V2.Account.Bank.GetBlob(stoppingToken);
        return bank is null ? 0 : await SyncContainerAsync(accountId, AccountContainer.Bank, bank, now, stoppingToken);
    }

    private async Task<int> SyncSharedInventoryAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountItem[]? inventory = await client.V2.Account.Inventory.GetBlob(stoppingToken);
        return inventory is null ? 0 : await SyncContainerAsync(accountId, AccountContainer.SharedInventory, inventory, now, stoppingToken);
    }

    private async Task<int> SyncCharactersAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        // One full-character request per character (1 + N) — the heavy object carries details AND bags, so no
        // separate inventory call is needed. Rides the shared account-sync pass.
        string[]? characterNames = await client.V2.Characters.GetIds(stoppingToken);
        if (characterNames is null || characterNames.Length == 0)
        {
            return 0;
        }

        // Account-wide per-item totals, summed across every character's bags, so moving an item between characters
        // isn't a gain or loss in the observation log.
        var totals = new Dictionary<int, int>();
        foreach (string characterName in characterNames)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            ApiChar.Character? character;
            try
            {
                character = await client.V2.Characters.GetById(characterName, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Character sync failed for {Character} on {Account}; skipping it.", characterName, accountId);
                continue;
            }

            if (character is null)
            {
                continue;
            }

            // (a) Upsert the character's details — the source of the character list.
            await UpsertCharacterAsync(accountId, character, stoppingToken);

            // (b) Replace this character's bag slot grid (per-character, so one character's failure leaves the
            // others' snapshots intact). ExecuteDelete runs now; the re-inserts commit with the observation save.
            await _dbContext
                .CharacterItemSlots.Where(s => s.AccountId == accountId && s.CharacterName == characterName)
                .ExecuteDeleteAsync(stoppingToken);

            var slotIndex = 0;
            foreach (ApiChar.CharacterInventoryBag? bag in character.Bags)
            {
                if (bag is null)
                {
                    continue;
                }

                foreach (Api.AccountItem? item in bag.Inventory)
                {
                    _dbContext.CharacterItemSlots.Add(
                        new CharacterItemSlot
                        {
                            AccountId = accountId,
                            CharacterName = characterName,
                            SlotIndex = slotIndex,
                            ItemId = item?.Id,
                            Count = item?.Count ?? 0,
                            Charges = item?.Charges,
                        }
                    );
                    slotIndex++;

                    // (c) Accumulate the account-wide per-item total.
                    if (item is not null)
                    {
                        totals[item.Id] = totals.GetValueOrDefault(item.Id) + item.Count;
                    }
                }
            }
        }

        await SyncItemObservationsAsync(accountId, AccountContainer.CharacterInventory, totals, now, stoppingToken);
        return characterNames.Length;
    }

    private async Task UpsertCharacterAsync(string accountId, ApiChar.Character character, CancellationToken stoppingToken)
    {
        Character? existing = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.AccountId == accountId && c.Name == character.Name, stoppingToken);
        if (existing is null)
        {
            existing = new Character { AccountId = accountId, Name = character.Name };
            _dbContext.Characters.Add(existing);
        }

        existing.Race = character.Race.ToString();
        existing.Gender = character.Gender.ToString();
        existing.Profession = character.Profession.ToString();
        existing.Level = character.Level;
        existing.Guild = character.Guild;
        existing.Age = character.Age;
        existing.Created = character.Created;
        existing.LastModified = character.LastModified;
        existing.Deaths = character.Deaths;
        existing.Title = character.Title;
        existing.BuildTabsUnlocked = character.BuildTabsUnlocked;
        existing.ActiveBuildTab = character.ActiveBuildTab;
        existing.EquipmentTabsUnlocked = character.EquipmentTabsUnlocked;
        existing.ActiveEquipmentTab = character.ActiveEquipmentTab;
        // SaveChanges is deferred to the section's SyncItemObservationsAsync call, committing these upserts with
        // the pending slot inserts and observation rows together.
    }

    private async Task<int> SyncContainerAsync(
        string accountId,
        string store,
        IReadOnlyList<Api.AccountItem?> slots,
        DateTimeOffset now,
        CancellationToken stoppingToken
    )
    {
        // (a) Replace the slot grid for an exact current layout. ExecuteDelete runs immediately, so re-inserting
        // the same (AccountId, Store, SlotIndex) keys in the following SaveChanges can't collide.
        await _dbContext
            .AccountContainerSlots.Where(s => s.AccountId == accountId && s.Store == store)
            .ExecuteDeleteAsync(stoppingToken);

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            Api.AccountItem? item = slots[slotIndex];
            _dbContext.AccountContainerSlots.Add(
                new AccountContainerSlot
                {
                    AccountId = accountId,
                    Store = store,
                    SlotIndex = slotIndex,
                    ItemId = item?.Id,
                    Count = item?.Count ?? 0,
                    Charges = item?.Charges,
                }
            );
        }

        // (b) Per-item holdings (summed across slots), appended on change for future deltas.
        Dictionary<int, int> totals = slots
            .Where(s => s is not null)
            .GroupBy(s => s!.Id)
            .ToDictionary(g => g.Key, g => g.Sum(s => s!.Count));

        await SyncItemObservationsAsync(accountId, store, totals, now, stoppingToken);
        return totals.Count;
    }

    /// <summary>
    /// Appends an <see cref="AccountItemObservation"/> for each item whose per-item total changed since this
    /// container's last observation, plus a zero row for any previously-held item that's now gone. Shared by every
    /// item section — material storage, bank, shared inventory, and character bags — and persists pending changes.
    /// </summary>
    private async Task SyncItemObservationsAsync(
        string accountId,
        string container,
        IReadOnlyDictionary<int, int> totals,
        DateTimeOffset now,
        CancellationToken stoppingToken
    )
    {
        IQueryable<long> maxIds = _dbContext
            .AccountItemObservations.Where(o => o.AccountId == accountId && o.Container == container)
            .GroupBy(o => o.ItemId)
            .Select(g => g.Max(x => x.Id));
        Dictionary<int, int> latest = await _dbContext
            .AccountItemObservations.Where(o => maxIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.ItemId, o => o.Count, stoppingToken);

        foreach ((int itemId, int count) in totals)
        {
            if (!latest.TryGetValue(itemId, out int previous) || previous != count)
            {
                _dbContext.AccountItemObservations.Add(
                    new AccountItemObservation
                    {
                        AccountId = accountId,
                        Container = container,
                        ItemId = itemId,
                        Count = count,
                        ObservedAtUtc = now,
                    }
                );
            }
        }

        foreach ((int itemId, int count) in latest)
        {
            if (count != 0 && !totals.ContainsKey(itemId))
            {
                _dbContext.AccountItemObservations.Add(
                    new AccountItemObservation
                    {
                        AccountId = accountId,
                        Container = container,
                        ItemId = itemId,
                        Count = 0,
                        ObservedAtUtc = now,
                    }
                );
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
    }
}
