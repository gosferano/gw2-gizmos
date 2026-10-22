using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Api = Gw2Gizmos.Gw2Api.Contract.V2.Account;
// Aliased so the contract's Character doesn't collide with the Character entity (Entities.Accounts).
using ApiChar = Gw2Gizmos.Gw2Api.Contract.V2.Characters;
using ApiCommerce = Gw2Gizmos.Gw2Api.Contract.V2.Commerce;
using ApiDungeons = Gw2Gizmos.Gw2Api.Contract.V2.Dungeons;
using ApiRaids = Gw2Gizmos.Gw2Api.Contract.V2.Raids;
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

    // The running sync's token, so a section cut short by shutdown isn't reported as an API failure.
    private CancellationToken _stoppingToken;

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

    /// <summary>The regular sync: everything enabled, stamped now.</summary>
    public Task SyncAccount(CancellationToken stoppingToken) => SyncAsync(SyncScope.Full, stoppingToken);

    /// <summary>
    /// The sync at a play-session boundary: only what can change during a sitting — wallet, material storage, bank,
    /// shared inventory and the given characters' bags (the others' stored bags still count towards the totals) — so
    /// it takes seconds, not the full sync's minute. Its observations are stamped <paramref name="observedAt"/>, the
    /// moment the boundary happened, so the session's holdings at that boundary include them however long the sync
    /// took.
    /// </summary>
    public Task SyncHoldingsAt(DateTimeOffset observedAt, IEnumerable<string> characters, CancellationToken stoppingToken) =>
        SyncAsync(new SyncScope(observedAt, characters.ToHashSet(StringComparer.Ordinal)), stoppingToken);

    /// <summary>What a sync covers: everything (<see cref="Full"/>), or a session boundary's holdings.</summary>
    private sealed record SyncScope(DateTimeOffset? ObservedAt, IReadOnlySet<string>? Characters)
    {
        public static SyncScope Full { get; } = new(null, null);

        public bool IsBoundary => ObservedAt is not null;
    }

    private async Task SyncAsync(SyncScope scope, CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
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
                string? name = await SyncOneAccountAsync(apiKey, seenAccounts, scope, stoppingToken);
                if (name is not null)
                {
                    syncedNames.Add(name);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down (e.g. the app closing to update): not a failure.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Account sync failed for one API key; continuing with the others.");
            }
        }

        _logger.LogInformation(
            scope.IsBoundary
                ? "Session-boundary sync completed for {Count} account(s): {Accounts}."
                : "Account sync completed for {Count} account(s): {Accounts}.",
            syncedNames.Count,
            syncedNames.Count == 0 ? "(none)" : string.Join(", ", syncedNames)
        );
    }

    /// <summary>Syncs the account for one key; returns its name, or null when skipped (no data, or a duplicate
    /// account already synced this run).</summary>
    private async Task<string?> SyncOneAccountAsync(
        string apiKey,
        HashSet<string> seenAccounts,
        SyncScope scope,
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

        // A boundary sync records its observations at the boundary; the account's "synced" time is when it ran.
        DateTimeOffset syncedAt = _timeProvider.GetUtcNow();
        DateTimeOffset now = scope.ObservedAt ?? syncedAt;
        await UpsertAccountAsync(account, syncedAt, stoppingToken);

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
        await RunSection(WorkerFeatures.CharacterInventory, account, permissions, () => SyncCharactersAsync(client, account.Id, now, scope.Characters, stoppingToken));
        if (!scope.IsBoundary)
        {
            // The trading post doesn't change what a session earned in-game, so a boundary sync leaves it.
            await RunSection(WorkerFeatures.TradingPost, account, permissions, () => SyncTradingPostAsync(client, account.Id, now, stoppingToken));
            // Nor the recipes learned.
            await RunSection(
                WorkerFeatures.LearnedRecipes,
                account,
                permissions,
                () => SyncLearnedRecipesAsync(client, account.Id, stoppingToken)
            );
            // Nor the wardrobe's unlocks.
            await RunSection(
                WorkerFeatures.WardrobeUnlocks,
                account,
                permissions,
                () => SyncWardrobeUnlocksAsync(client, account.Id, stoppingToken)
            );
            // Nor achievement progress.
            await RunSection(
                WorkerFeatures.Achievements,
                account,
                permissions,
                () => SyncAchievementsAsync(client, account.Id, now, stoppingToken)
            );
            // Nor the reset's dailies and weeklies.
            await RunSection(
                WorkerFeatures.ResetProgress,
                account,
                permissions,
                () => SyncResetProgressAsync(client, account.Id, now, stoppingToken)
            );
        }

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
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            // Shutting down: stop the sync here rather than warn about each remaining section.
            _dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (Exception ex)
        {
            // Drop whatever the failed section left pending, so the next section's save doesn't commit half of it.
            _dbContext.ChangeTracker.Clear();
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

    // The API's largest page for the transaction endpoints.
    private const int TransactionPageSize = 200;

    /// <summary>
    /// Trading post: replaces the account's open orders (each with its item's current best buy/sell prices, so the
    /// UI can flag undercut and outbid orders) and adds completed trades not stored yet. History comes newest first,
    /// so after the first sync (which reads all of the API's 90 days) paging stops at the first trade already
    /// stored — normally a single page. Returns the open orders plus the new trades.
    /// </summary>
    private async Task<int> SyncAchievementsAsync(
        Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        // Replaced wholesale: the API lists every achievement with progress, so what's stored is exactly that list.
        Api.AccountAchievement[]? progress = await client.V2.Account.Achievements.GetBlob(stoppingToken);
        if (progress is null)
        {
            return 0;
        }

        await _dbContext.AccountAchievements.Where(a => a.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        foreach (Api.AccountAchievement achievement in progress.DistinctBy(a => a.Id))
        {
            _dbContext.AccountAchievements.Add(new AccountAchievement
            {
                AccountId = accountId,
                AchievementId = achievement.Id,
                Current = achievement.Current,
                Max = achievement.Max,
                Done = achievement.Done,
                Bits = achievement.Bits,
                Repeated = achievement.Repeated,
                Unlocked = achievement.Unlocked,
                SyncedAtUtc = now,
            });
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        return progress.Length;
    }

    private async Task<int> SyncLearnedRecipesAsync(
        Gw2ApiClient client, string accountId, CancellationToken stoppingToken)
    {
        // Replaced wholesale: the API lists every recipe the account knows.
        int[]? recipeIds = await client.V2.Account.Recipes.GetBlob(stoppingToken);
        if (recipeIds is null)
        {
            return 0;
        }

        await _dbContext.AccountRecipes.Where(r => r.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        _dbContext.AccountRecipes.AddRange(
            recipeIds.Distinct().Select(id => new AccountRecipe { AccountId = accountId, RecipeId = id }));
        await _dbContext.SaveChangesAsync(stoppingToken);
        return recipeIds.Length;
    }

    /// <summary>
    /// The dyes, minis and skins the account has unlocked. Each kind is replaced on its own, and only when its request
    /// succeeds, so one failing endpoint leaves its last known state. Returns the rows stored.
    /// </summary>
    private async Task<int> SyncWardrobeUnlocksAsync(
        Gw2ApiClient client, string accountId, CancellationToken stoppingToken)
    {
        int rows = 0;
        rows += await StoreUnlocksAsync(accountId, AccountUnlockKind.Dye,
            await client.V2.Account.Dyes.GetBlob(stoppingToken), stoppingToken);
        rows += await StoreUnlocksAsync(accountId, AccountUnlockKind.Mini,
            await client.V2.Account.Minis.GetBlob(stoppingToken), stoppingToken);
        rows += await StoreUnlocksAsync(accountId, AccountUnlockKind.Skin,
            await client.V2.Account.Skins.GetBlob(stoppingToken), stoppingToken);
        return rows;
    }

    private async Task<int> StoreUnlocksAsync(
        string accountId, string kind, int[]? unlockIds, CancellationToken stoppingToken)
    {
        if (unlockIds is null)
        {
            return 0;
        }

        await _dbContext.AccountUnlocks.Where(u => u.AccountId == accountId && u.Kind == kind)
            .ExecuteDeleteAsync(stoppingToken);
        _dbContext.AccountUnlocks.AddRange(unlockIds.Distinct()
            .Select(id => new AccountUnlock { AccountId = accountId, Kind = kind, UnlockId = id }));
        await _dbContext.SaveChangesAsync(stoppingToken);
        return unlockIds.Length;
    }

    /// <summary>
    /// The account's progress for the current reset: the Wizard's Vault's daily, weekly and special objectives, and
    /// which world bosses, map chests, daily crafts, raid encounters and dungeon paths are done. Each part is replaced
    /// on its own, and only when its request succeeds, so one failing endpoint leaves its last known state. Returns
    /// the rows stored.
    /// </summary>
    private async Task<int> SyncResetProgressAsync(
        Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        int rows = 0;
        rows += await StoreWizardsVaultAsync(accountId, WizardsVaultPeriod.Daily,
            await client.V2.Account.WizardsVault.Daily.GetBlob(stoppingToken), now, stoppingToken);
        rows += await StoreWizardsVaultAsync(accountId, WizardsVaultPeriod.Weekly,
            await client.V2.Account.WizardsVault.Weekly.GetBlob(stoppingToken), now, stoppingToken);
        Api.AccountWizardsVaultSpecial? special = await client.V2.Account.WizardsVault.Special.GetBlob(stoppingToken);
        if (special is not null)
        {
            rows += await StoreWizardsVaultObjectivesAsync(
                accountId, WizardsVaultPeriod.Special, special.Objectives, now, stoppingToken);
        }

        // Each kind's master list (every activity, in the API's order) with the account's done list.
        rows += await StoreActivitiesAsync(accountId, ResetActivityKind.WorldBoss,
            ToActivities(await client.V2.WorldBosses.GetIds(stoppingToken)),
            await client.V2.Account.WorldBosses.GetBlob(stoppingToken), now, stoppingToken);
        rows += await StoreActivitiesAsync(accountId, ResetActivityKind.MapChest,
            ToActivities(await client.V2.MapChests.GetIds(stoppingToken)),
            await client.V2.Account.MapChests.GetBlob(stoppingToken), now, stoppingToken);
        rows += await StoreActivitiesAsync(accountId, ResetActivityKind.DailyCrafting,
            ToActivities(await client.V2.DailyCrafting.GetIds(stoppingToken)),
            await client.V2.Account.DailyCrafting.GetBlob(stoppingToken), now, stoppingToken);
        ApiRaids.Raid[]? raids = await client.V2.Raids.GetAll(stoppingToken);
        rows += await StoreActivitiesAsync(accountId, ResetActivityKind.Raid,
            raids?.SelectMany(raid => raid.Wings)
                .SelectMany(wing => wing.Events.Select(encounter =>
                    (encounter.Id, (string?)wing.Id, encounter.Type == ApiRaids.RaidWingEventType.Checkpoint)))
                .ToArray(),
            await client.V2.Account.Raids.GetBlob(stoppingToken), now, stoppingToken);
        ApiDungeons.Dungeon[]? dungeons = await client.V2.Dungeons.GetAll(stoppingToken);
        rows += await StoreActivitiesAsync(accountId, ResetActivityKind.Dungeon,
            dungeons?.SelectMany(dungeon => dungeon.Paths.Select(path => (path.Id, (string?)dungeon.Id, false)))
                .ToArray(),
            await client.V2.Account.Dungeons.GetBlob(stoppingToken), now, stoppingToken);
        return rows;
    }

    private static (string Id, string? Group, bool IsCheckpoint)[]? ToActivities(string[]? ids) =>
        ids?.Select(id => (id, (string?)null, false)).ToArray();

    private async Task<int> StoreWizardsVaultAsync(
        string accountId, string period, Api.AccountWizardsVaultCategory? category, DateTimeOffset now,
        CancellationToken stoppingToken)
    {
        if (category is null)
        {
            return 0;
        }

        AccountWizardsVaultMeta? stored = await _dbContext.AccountWizardsVaultMetas.AsNoTracking()
            .FirstOrDefaultAsync(m => m.AccountId == accountId && m.Period == period, stoppingToken);
        Dictionary<int, AccountWizardsVaultObjective> storedObjectives = await _dbContext.AccountWizardsVaultObjectives
            .AsNoTracking()
            .Where(o => o.AccountId == accountId && o.Period == period)
            .ToDictionaryAsync(o => o.ObjectiveId, stoppingToken);
        DateTimeOffset currentSince = stored is not null && IsSameSet(stored, storedObjectives, category)
            ? stored.CurrentSinceUtc
            : now;

        await _dbContext.AccountWizardsVaultMetas.Where(m => m.AccountId == accountId && m.Period == period)
            .ExecuteDeleteAsync(stoppingToken);
        _dbContext.AccountWizardsVaultMetas.Add(new AccountWizardsVaultMeta
        {
            AccountId = accountId,
            Period = period,
            ProgressCurrent = category.MetaProgressCurrent,
            ProgressComplete = category.MetaProgressComplete,
            RewardItemId = category.MetaRewardItemId,
            RewardAstral = category.MetaRewardAstral,
            RewardClaimed = category.MetaRewardClaimed,
            SyncedAtUtc = now,
            CurrentSinceUtc = currentSince,
        });
        return 1 + await StoreWizardsVaultObjectivesAsync(accountId, period, category.Objectives, now, stoppingToken);
    }

    /// <summary>
    /// Whether the API's set is the one stored: the same objectives, with nothing gone back down (progress, a claim,
    /// the meta reward) — which only a new set does, even one that happens to repeat the same objectives.
    /// </summary>
    private static bool IsSameSet(
        AccountWizardsVaultMeta stored, IReadOnlyDictionary<int, AccountWizardsVaultObjective> storedObjectives,
        Api.AccountWizardsVaultCategory category) =>
        category.Objectives.Length == storedObjectives.Count
        && category.Objectives.All(o => storedObjectives.TryGetValue(o.Id, out AccountWizardsVaultObjective? was)
            && o.ProgressCurrent >= was.ProgressCurrent && (o.Claimed || !was.Claimed))
        && category.MetaProgressCurrent >= stored.ProgressCurrent
        && (category.MetaRewardClaimed || !stored.RewardClaimed);

    private async Task<int> StoreWizardsVaultObjectivesAsync(
        string accountId, string period, Api.AccountWizardsVaultObjective[] objectives, DateTimeOffset now,
        CancellationToken stoppingToken)
    {
        await _dbContext.AccountWizardsVaultObjectives.Where(o => o.AccountId == accountId && o.Period == period)
            .ExecuteDeleteAsync(stoppingToken);
        foreach (Api.AccountWizardsVaultObjective objective in objectives.DistinctBy(o => o.Id))
        {
            _dbContext.AccountWizardsVaultObjectives.Add(new AccountWizardsVaultObjective
            {
                AccountId = accountId,
                Period = period,
                ObjectiveId = objective.Id,
                Title = objective.Title,
                Track = objective.Track,
                Acclaim = objective.Acclaim,
                ProgressCurrent = objective.ProgressCurrent,
                ProgressComplete = objective.ProgressComplete,
                Claimed = objective.Claimed,
                SyncedAtUtc = now,
            });
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        return objectives.Length;
    }

    /// <summary>
    /// Replaces an account's activities of one kind: every activity of the master list, done when the account's list
    /// has it. Left as they were when either request failed.
    /// </summary>
    private async Task<int> StoreActivitiesAsync(
        string accountId, string kind, (string Id, string? Group, bool IsCheckpoint)[]? all, string[]? done,
        DateTimeOffset now, CancellationToken stoppingToken)
    {
        if (all is null || done is null)
        {
            return 0;
        }

        var doneIds = new HashSet<string>(done, StringComparer.Ordinal);
        await _dbContext.AccountResetActivities.Where(a => a.AccountId == accountId && a.Kind == kind)
            .ExecuteDeleteAsync(stoppingToken);
        int order = 0;
        foreach ((string id, string? group, bool isCheckpoint) in all.DistinctBy(activity => activity.Id))
        {
            _dbContext.AccountResetActivities.Add(new AccountResetActivity
            {
                AccountId = accountId,
                Kind = kind,
                ActivityId = id,
                Group = group,
                Order = order++,
                IsCheckpoint = isCheckpoint,
                Done = doneIds.Contains(id),
                SyncedAtUtc = now,
            });
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        return order;
    }

    private async Task<int> SyncTradingPostAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        // Open orders, replaced wholesale.
        List<(string Side, ApiCommerce.CommerceTransaction Order)> open = new();
        foreach (ApiCommerce.CommerceTransaction order in await ReadPagesAsync(
                     async page => await client.V2.Commerce.Transactions.Current.Buys.GetPage(page, TransactionPageSize, stoppingToken),
                     stopAt: null))
        {
            open.Add((TradingPostSide.Buy, order));
        }

        foreach (ApiCommerce.CommerceTransaction order in await ReadPagesAsync(
                     async page => await client.V2.Commerce.Transactions.Current.Sells.GetPage(page, TransactionPageSize, stoppingToken),
                     stopAt: null))
        {
            open.Add((TradingPostSide.Sell, order));
        }

        Dictionary<int, ApiCommerce.CommercePrices> prices = new();
        foreach (int[] chunk in open.Select(o => o.Order.ItemId).Distinct().Chunk(TransactionPageSize))
        {
            ApiCommerce.CommercePrices[]? page = await client.V2.Commerce.Prices.GetByIds(chunk, stoppingToken);
            foreach (ApiCommerce.CommercePrices price in page ?? [])
            {
                prices[price.Id] = price;
            }
        }

        await _dbContext.TradingPostOrders.Where(o => o.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        foreach ((string side, ApiCommerce.CommerceTransaction order) in open)
        {
            ApiCommerce.CommercePrices? price = prices.GetValueOrDefault(order.ItemId);
            _dbContext.TradingPostOrders.Add(new TradingPostOrder
            {
                Id = order.Id,
                AccountId = accountId,
                Side = side,
                ItemId = order.ItemId,
                Price = order.Price,
                Quantity = order.Quantity,
                CreatedUtc = order.Created,
                // 0 means nobody is buying / nothing is listed.
                BestBuyPrice = price?.Buys is { Quantity: > 0 } buys ? buys.UnitPrice : null,
                BestSellPrice = price?.Sells is { Quantity: > 0 } sells ? sells.UnitPrice : null,
                SyncedAtUtc = now,
            });
        }

        // Completed trades: only the ones not stored yet.
        HashSet<long> known = (await _dbContext.TradingPostTransactions
                .Where(t => t.AccountId == accountId)
                .Select(t => t.Id)
                .ToListAsync(stoppingToken))
            .ToHashSet();
        int added = 0;
        foreach ((string side, Func<int, Task<ApiCommerce.CommerceTransaction[]?>> getPage) in new[]
                 {
                     (TradingPostSide.Buy, (Func<int, Task<ApiCommerce.CommerceTransaction[]?>>)(async page =>
                         await client.V2.Commerce.Transactions.History.Buys.GetPage(page, TransactionPageSize, stoppingToken))),
                     (TradingPostSide.Sell, async page =>
                         await client.V2.Commerce.Transactions.History.Sells.GetPage(page, TransactionPageSize, stoppingToken)),
                 })
        {
            foreach (ApiCommerce.CommerceTransaction trade in await ReadPagesAsync(getPage, stopAt: known))
            {
                if (trade.Purchased is not { } purchased || !known.Add(trade.Id))
                {
                    continue;
                }

                _dbContext.TradingPostTransactions.Add(new TradingPostTransaction
                {
                    Id = trade.Id,
                    AccountId = accountId,
                    Side = side,
                    ItemId = trade.ItemId,
                    Price = trade.Price,
                    Quantity = trade.Quantity,
                    CreatedUtc = trade.Created,
                    PurchasedUtc = purchased,
                });
                added++;
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        return open.Count + added;
    }

    /// <summary>
    /// Reads a paged transaction endpoint from page 0 until a short (or missing) page — or, with
    /// <paramref name="stopAt"/>, until a page contains an id already known (history is newest first).
    /// </summary>
    private static async Task<List<ApiCommerce.CommerceTransaction>> ReadPagesAsync(
        Func<int, Task<ApiCommerce.CommerceTransaction[]?>> getPage, IReadOnlySet<long>? stopAt)
    {
        var all = new List<ApiCommerce.CommerceTransaction>();
        for (int page = 0; ; page++)
        {
            ApiCommerce.CommerceTransaction[]? items = await getPage(page);
            if (items is null || items.Length == 0)
            {
                break;
            }

            all.AddRange(items);
            if (items.Length < TransactionPageSize || (stopAt is not null && items.Any(t => stopAt.Contains(t.Id))))
            {
                break;
            }
        }

        return all;
    }

    private async Task<int> SyncSharedInventoryAsync(Gw2ApiClient client, string accountId, DateTimeOffset now, CancellationToken stoppingToken)
    {
        Api.AccountItem[]? inventory = await client.V2.Account.Inventory.GetBlob(stoppingToken);
        return inventory is null ? 0 : await SyncContainerAsync(accountId, AccountContainer.SharedInventory, inventory, now, stoppingToken);
    }

    /// <param name="only">Just these characters (the rest keep their stored bags, which still count), or all when null.</param>
    private async Task<int> SyncCharactersAsync(
        Gw2ApiClient client, string accountId, DateTimeOffset now, IReadOnlySet<string>? only, CancellationToken stoppingToken)
    {
        // One full-character request per character (1 + N) — the heavy object carries details AND bags, so no
        // separate inventory call is needed. Rides the shared account-sync pass.
        string[]? characterNames = await client.V2.Characters.GetIds(stoppingToken);
        if (characterNames is null || characterNames.Length == 0)
        {
            return 0;
        }

        // Fetch every character before touching the database: the slow part (one request per character) then can't
        // be cut short halfway through replacing the bags — an interrupted sync (the app quitting, the worker
        // restarting) leaves the stored bags as they were.
        var characters = new List<ApiChar.Character>();
        var unfetched = new List<string>();
        foreach (string characterName in characterNames)
        {
            stoppingToken.ThrowIfCancellationRequested();
            if (only is not null && !only.Contains(characterName))
            {
                unfetched.Add(characterName);
                continue;
            }

            ApiChar.Character? character;
            try
            {
                character = await client.V2.Characters.GetById(characterName, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Character sync failed for {Character} on {Account}; skipping it.", characterName, accountId);
                character = null;
            }

            if (character is null)
            {
                unfetched.Add(characterName);
                continue;
            }

            characters.Add(character);
        }

        // Then replace the bags and log the holdings in one transaction, so they're all saved or none are.
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(stoppingToken);

        // Account-wide per-item totals, summed across every character's bags, so moving an item between characters
        // isn't a gain or loss in the observation log.
        var totals = new Dictionary<int, int>();
        foreach (ApiChar.Character character in characters)
        {
            string characterName = character.Name;

            // (a) Upsert the character's details — the source of the character list.
            await UpsertCharacterAsync(accountId, character, stoppingToken);

            // (b) Replace this character's bag slot grid. ExecuteDelete runs now (inside the transaction); the
            // re-inserts commit with the observation save.
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
                            Binding = item?.Binding?.Value,
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

        // A character not fetched — its request failed, or a session-boundary sync left it out — keeps its stored
        // bags; count those too, so its items don't read as lost now and gained back on the next sync.
        if (unfetched.Count > 0)
        {
            var kept = await _dbContext
                .CharacterItemSlots.Where(s =>
                    s.AccountId == accountId && unfetched.Contains(s.CharacterName) && s.ItemId != null)
                .GroupBy(s => s.ItemId!.Value)
                .Select(g => new { ItemId = g.Key, Count = g.Sum(s => s.Count) })
                .ToListAsync(stoppingToken);
            foreach (var item in kept)
            {
                totals[item.ItemId] = totals.GetValueOrDefault(item.ItemId) + item.Count;
            }
        }

        await SyncItemObservationsAsync(accountId, AccountContainer.CharacterInventory, totals, now, stoppingToken);
        await transaction.CommitAsync(stoppingToken);
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
        // build_tabs only come with the key's builds permission; without it the specializations stay empty.
        ApiChar.CharacterBuildTab? activeBuild =
            character.BuildTabs.FirstOrDefault(t => t.IsActive)
            ?? character.BuildTabs.FirstOrDefault(t => t.Tab == character.ActiveBuildTab);
        existing.Specializations = activeBuild?.Build?.Specializations
            .Where(s => s?.Id is not null)
            .Select(s => s.Id!.Value)
            .ToArray() ?? [];
        existing.Crafting = character.Crafting
            .Select(c => new CharacterCrafting { Discipline = c.Discipline.ToString(), Rating = c.Rating, Active = c.Active })
            .ToList();
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
        // The grid replacement and the holdings log are saved together or not at all.
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(stoppingToken);

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
                    Binding = item?.Binding?.Value,
                }
            );
        }

        // (b) Per-item holdings (summed across slots), appended on change for future deltas.
        Dictionary<int, int> totals = slots
            .Where(s => s is not null)
            .GroupBy(s => s!.Id)
            .ToDictionary(g => g.Key, g => g.Sum(s => s!.Count));

        await SyncItemObservationsAsync(accountId, store, totals, now, stoppingToken);
        await transaction.CommitAsync(stoppingToken);
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
