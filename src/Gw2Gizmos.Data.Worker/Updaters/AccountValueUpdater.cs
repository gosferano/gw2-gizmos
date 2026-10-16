using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Records each recently-synced account's value (<see cref="AccountValuation"/>) after the account sync, keeping one
/// point per hour: a later sync in the same hour updates that hour's point. The first time an account is valued, its
/// history is backfilled first — one point per day (at UTC midnight) since its holdings logs begin, valued at each
/// day's prices where the price history has them.
/// </summary>
public class AccountValueUpdater
{
    // Only accounts the sync reached lately: a removed key's account stops being valued.
    private static readonly TimeSpan RecentlySynced = TimeSpan.FromHours(1);

    private const int MaxBackfillDays = 365;

    private readonly Gw2GizmosDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountValueUpdater> _logger;

    public AccountValueUpdater(Gw2GizmosDbContext dbContext, TimeProvider timeProvider, ILogger<AccountValueUpdater> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RecordAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset since = now - RecentlySynced;
        List<string> accountIds = await _dbContext.Accounts
            .Where(a => a.LastSyncedUtc >= since)
            .Select(a => a.AccountId)
            .ToListAsync(stoppingToken);

        foreach (string accountId in accountIds)
        {
            stoppingToken.ThrowIfCancellationRequested();
            if (!await _dbContext.AccountValueSnapshots.AnyAsync(s => s.AccountId == accountId, stoppingToken))
            {
                int days = Backfill(accountId, now);
                _logger.LogInformation("Backfilled {Days} day(s) of account value for {Account}.", days, accountId);
            }

            RecordNow(accountId, now);
            await _dbContext.SaveChangesAsync(stoppingToken);
        }
    }

    /// <summary>Adds one point per UTC midnight since the account's holdings logs begin; returns how many.</summary>
    private int Backfill(string accountId, DateTimeOffset now)
    {
        DateTimeOffset? firstItem = _dbContext.AccountItemObservations
            .Where(o => o.AccountId == accountId)
            .Min(o => (DateTimeOffset?)o.ObservedAtUtc);
        DateTimeOffset? firstWallet = _dbContext.AccountWalletObservations
            .Where(o => o.AccountId == accountId)
            .Min(o => (DateTimeOffset?)o.ObservedAtUtc);
        if ((firstItem ?? firstWallet) is null)
        {
            return 0;
        }

        DateTimeOffset first = new[] { firstItem, firstWallet }.Where(t => t is not null).Min()!.Value;
        DateTimeOffset day = new DateTimeOffset(first.UtcDateTime.Date, TimeSpan.Zero).AddDays(1);
        DateTimeOffset earliest = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).AddDays(-MaxBackfillDays);
        if (day < earliest)
        {
            day = earliest;
        }

        int added = 0;
        for (; day < now; day = day.AddDays(1))
        {
            AccountValue value = AccountValuation.HoldingsAsOf(_dbContext, accountId, day, historicalPrices: true);
            _dbContext.AccountValueSnapshots.Add(ToSnapshot(accountId, day, value, isBackfilled: true));
            added++;
        }

        return added;
    }

    /// <summary>Values the account now, updating this hour's point if there is one.</summary>
    private void RecordNow(string accountId, DateTimeOffset now)
    {
        AccountValue value = AccountValuation.HoldingsAsOf(_dbContext, accountId, now, historicalPrices: false)
            with { TradingPost = AccountValuation.TradingPostValue(_dbContext, accountId) };
        if (value.Total == 0)
        {
            // Nothing synced to value (e.g. every holdings feature off) — no point in a flat zero line.
            return;
        }

        var hourStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        AccountValueSnapshot? thisHour = _dbContext.AccountValueSnapshots
            .Where(s => s.AccountId == accountId && !s.IsBackfilled && s.TakenAtUtc >= hourStart)
            .OrderByDescending(s => s.Id)
            .FirstOrDefault();
        if (thisHour is null)
        {
            _dbContext.AccountValueSnapshots.Add(ToSnapshot(accountId, now, value, isBackfilled: false));
            return;
        }

        AccountValueSnapshot updated = ToSnapshot(accountId, now, value, isBackfilled: false);
        updated.Id = thisHour.Id;
        _dbContext.Entry(thisHour).CurrentValues.SetValues(updated);
    }

    private static AccountValueSnapshot ToSnapshot(
        string accountId, DateTimeOffset takenAtUtc, AccountValue value, bool isBackfilled) =>
        new()
        {
            AccountId = accountId,
            TakenAtUtc = takenAtUtc,
            CoinCopper = value.Coin,
            MaterialsCopper = value.Materials,
            BankCopper = value.Bank,
            SharedInventoryCopper = value.SharedInventory,
            CharactersCopper = value.Characters,
            TradingPostCopper = value.TradingPost,
            IsBackfilled = isBackfilled,
        };
}
