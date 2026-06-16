using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.Worker.Features;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Deletes a category of locally-stored data on request from the desktop (see <see cref="DeletableData"/>). The
/// worker is the DB's sole writer, so these user-initiated deletes run here, not in the read-only desktop. Each
/// category maps to one or more table deletes (account-scoped ones filtered by account id).
/// </summary>
public class AccountDataDeleter
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<AccountDataDeleter> _logger;

    public AccountDataDeleter(Gw2GizmosDbContext dbContext, ILogger<AccountDataDeleter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task DeleteAsync(string typeKey, string? accountId, CancellationToken stoppingToken)
    {
        int rows;
        if (typeKey == DeletableData.PriceHistory)
        {
            rows = await _dbContext.PriceSnapshots.ExecuteDeleteAsync(stoppingToken);
        }
        else if (string.IsNullOrEmpty(accountId))
        {
            _logger.LogWarning("Delete '{Type}' is account-scoped but no account was given; skipping.", typeKey);
            return;
        }
        else
        {
            rows = await DeleteForAccountAsync(typeKey, accountId, stoppingToken);
        }

        _logger.LogInformation(
            "Deleted stored data '{Type}' for {Account}: {Rows} row(s).",
            typeKey,
            accountId ?? "(global)",
            rows
        );
    }

    private async Task<int> DeleteForAccountAsync(string typeKey, string accountId, CancellationToken stoppingToken) =>
        typeKey switch
        {
            DeletableData.Wallet => await DeleteWalletAsync(accountId, stoppingToken),
            DeletableData.Materials => await DeleteItemContainerAsync(accountId, AccountContainer.MaterialStorage, stoppingToken),
            DeletableData.Bank => await DeleteSlotStoreAsync(accountId, AccountContainer.Bank, stoppingToken),
            DeletableData.SharedInventory => await DeleteSlotStoreAsync(accountId, AccountContainer.SharedInventory, stoppingToken),
            DeletableData.Characters => await DeleteCharactersAsync(accountId, stoppingToken),
            DeletableData.Sessions => await DeleteSessionsAsync(accountId, stoppingToken),
            DeletableData.AllForAccount => await DeleteAllForAccountAsync(accountId, stoppingToken),
            _ => 0,
        };

    private async Task<int> DeleteWalletAsync(string accountId, CancellationToken stoppingToken) =>
        await _dbContext.AccountWalletObservations.Where(o => o.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);

    private async Task<int> DeleteItemContainerAsync(string accountId, string container, CancellationToken stoppingToken) =>
        await _dbContext.AccountItemObservations
            .Where(o => o.AccountId == accountId && o.Container == container)
            .ExecuteDeleteAsync(stoppingToken);

    // Bank / shared inventory: the per-item observation log AND the current slot grid.
    private async Task<int> DeleteSlotStoreAsync(string accountId, string store, CancellationToken stoppingToken)
    {
        int rows = await DeleteItemContainerAsync(accountId, store, stoppingToken);
        rows += await _dbContext.AccountContainerSlots
            .Where(s => s.AccountId == accountId && s.Store == store)
            .ExecuteDeleteAsync(stoppingToken);
        return rows;
    }

    // Characters: the roster, their bag slot grids, and the account-wide character-inventory observation log.
    private async Task<int> DeleteCharactersAsync(string accountId, CancellationToken stoppingToken)
    {
        int rows = await DeleteItemContainerAsync(accountId, AccountContainer.CharacterInventory, stoppingToken);
        rows += await _dbContext.CharacterItemSlots.Where(s => s.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        rows += await _dbContext.Characters.Where(c => c.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        return rows;
    }

    // Sessions: the per-character segments (by their session's account) then the sessions themselves.
    private async Task<int> DeleteSessionsAsync(string accountId, CancellationToken stoppingToken)
    {
        int rows = await _dbContext.CharacterSegments
            .Where(s => _dbContext.GameSessions.Any(g => g.Id == s.GameSessionId && g.AccountId == accountId))
            .ExecuteDeleteAsync(stoppingToken);
        rows += await _dbContext.GameSessions.Where(g => g.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        return rows;
    }

    private async Task<int> DeleteAllForAccountAsync(string accountId, CancellationToken stoppingToken)
    {
        int rows = await DeleteWalletAsync(accountId, stoppingToken);
        rows += await DeleteItemContainerAsync(accountId, AccountContainer.MaterialStorage, stoppingToken);
        rows += await DeleteSlotStoreAsync(accountId, AccountContainer.Bank, stoppingToken);
        rows += await DeleteSlotStoreAsync(accountId, AccountContainer.SharedInventory, stoppingToken);
        rows += await DeleteCharactersAsync(accountId, stoppingToken);
        rows += await DeleteSessionsAsync(accountId, stoppingToken);
        // The account identity row itself; re-created on the next sync.
        rows += await _dbContext.Accounts.Where(a => a.AccountId == accountId).ExecuteDeleteAsync(stoppingToken);
        return rows;
    }
}
