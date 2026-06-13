using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Api = Gw2Gizmos.Gw2Api.Contract.V2.Account;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Syncs the authenticated GW2 account for the current API key. Upserts the <see cref="Account"/> identity,
/// then records wallet and material-storage balances and bank/shared-inventory holdings as append-on-change
/// observation logs (latest row per resource = current value), and replaces the bank/shared-inventory slot
/// grids for an exact current layout. Every row is keyed by account id so multiple accounts coexist.
/// </summary>
public class AccountSyncUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly IGw2ApiClientFactory _apiClientFactory;
    private readonly IGw2ApiKeyProvider _apiKeyProvider;
    private readonly ILogger<AccountSyncUpdater> _logger;
    private bool _warnedNoKey;

    public AccountSyncUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        IGw2ApiKeyProvider apiKeyProvider,
        ILogger<AccountSyncUpdater> logger
    )
    {
        _dbContext = dbContext;
        _apiClientFactory = apiClientFactory;
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
    }

    public async Task SyncAccount(CancellationToken stoppingToken)
    {
        string? apiKey = _apiKeyProvider.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (!_warnedNoKey)
            {
                _logger.LogInformation("No GW2 API key available; account sync is idle until one is provided.");
                _warnedNoKey = true;
            }

            return;
        }

        _warnedNoKey = false;
        Gw2ApiClient client = _apiClientFactory.Create(apiKey, Locale.English);

        Api.Account? account = await client.V2.Account.GetBlob(stoppingToken);
        if (account is null || string.IsNullOrEmpty(account.Id))
        {
            _logger.LogWarning("Account endpoint returned no data; the API key may lack the 'account' scope.");
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        await UpsertAccountAsync(account, now, stoppingToken);

        // Each store is independent: a missing scope on one endpoint shouldn't abort the others.
        await RunSection("wallet", () => SyncWalletAsync(client, account.Id, now, stoppingToken));
        await RunSection("materials", () => SyncMaterialsAsync(client, account.Id, now, stoppingToken));
        await RunSection("bank", () => SyncBankAsync(client, account.Id, now, stoppingToken));
        await RunSection("shared inventory", () => SyncSharedInventoryAsync(client, account.Id, now, stoppingToken));

        _logger.LogInformation("Account sync completed for {Account}.", account.Name);
    }

    private async Task RunSection(string name, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Account {Section} sync failed (missing scope or API error); continuing.", name);
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

    private async Task SyncWalletAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountWalletCurrency[]? wallet = await client.V2.Account.Wallet.GetBlob(stoppingToken);
        if (wallet is null)
        {
            return;
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
    }

    private async Task SyncMaterialsAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountMaterial[]? materials = await client.V2.Account.Materials.GetBlob(stoppingToken);
        if (materials is null)
        {
            return;
        }

        IQueryable<long> maxIds = _dbContext
            .AccountMaterialObservations.Where(o => o.AccountId == accountId)
            .GroupBy(o => o.ItemId)
            .Select(g => g.Max(x => x.Id));
        Dictionary<int, int> latest = await _dbContext
            .AccountMaterialObservations.Where(o => maxIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.ItemId, o => o.Count, stoppingToken);

        foreach (Api.AccountMaterial material in materials)
        {
            if (!latest.TryGetValue(material.Id, out int previous) || previous != material.Count)
            {
                _dbContext.AccountMaterialObservations.Add(
                    new AccountMaterialObservation
                    {
                        AccountId = accountId,
                        ItemId = material.Id,
                        Category = material.Category,
                        Count = material.Count,
                        ObservedAtUtc = now,
                    }
                );
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task SyncBankAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountItem?[]? bank = await client.V2.Account.Bank.GetBlob(stoppingToken);
        if (bank is not null)
        {
            await SyncContainerAsync(accountId, AccountContainer.Bank, bank, now, stoppingToken);
        }
    }

    private async Task SyncSharedInventoryAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountItem[]? inventory = await client.V2.Account.Inventory.GetBlob(stoppingToken);
        if (inventory is not null)
        {
            await SyncContainerAsync(accountId, AccountContainer.SharedInventory, inventory, now, stoppingToken);
        }
    }

    private async Task SyncContainerAsync(
        string accountId,
        AccountContainer store,
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

        IQueryable<long> maxIds = _dbContext
            .AccountItemHoldingObservations.Where(o => o.AccountId == accountId && o.Store == store)
            .GroupBy(o => o.ItemId)
            .Select(g => g.Max(x => x.Id));
        Dictionary<int, int> latest = await _dbContext
            .AccountItemHoldingObservations.Where(o => maxIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.ItemId, o => o.Count, stoppingToken);

        foreach ((int itemId, int count) in totals)
        {
            if (!latest.TryGetValue(itemId, out int previous) || previous != count)
            {
                _dbContext.AccountItemHoldingObservations.Add(
                    new AccountItemHoldingObservation
                    {
                        AccountId = accountId,
                        Store = store,
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
                _dbContext.AccountItemHoldingObservations.Add(
                    new AccountItemHoldingObservation
                    {
                        AccountId = accountId,
                        Store = store,
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
