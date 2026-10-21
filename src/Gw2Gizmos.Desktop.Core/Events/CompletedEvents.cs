using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Which scheduled events (world bosses, and metas whose reward is a map chest the API tracks) the account has done
/// this reset, from the synced reset progress: the Events page marks and can hide them, and reminders skip them. The
/// stored rows are re-read in the background at most every <see cref="MaxAge"/>; until a sync after the daily reset,
/// nothing reads as done.
/// </summary>
public sealed class CompletedEvents
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SelectedAccountService _account;
    private volatile ResetProgressData _data = ResetProgressData.Empty;
    private DateTimeOffset _readAt = DateTimeOffset.MinValue;
    private int _reading;

    public CompletedEvents(IServiceScopeFactory scopeFactory, SelectedAccountService account)
    {
        _scopeFactory = scopeFactory;
        _account = account;
    }

    /// <summary>
    /// Whether any completion is synced: without it (the feature off, no permission) nothing can read as done.
    /// </summary>
    public bool HasData => _data.Activities.Count > 0;

    /// <summary>The ids of the scheduled events done this reset (all three Convergences once their chest is).</summary>
    public IReadOnlySet<string> DoneEventIds(DateTimeOffset now)
    {
        if (now - _readAt >= MaxAge)
        {
            _ = RefreshAsync(now);
        }

        ResetProgressData data = _data;
        return new[] { ResetActivityKind.WorldBoss, ResetActivityKind.MapChest }
            .SelectMany(kind => ResetProgress.Activities(data, kind, now))
            .Where(activity => activity.Done)
            .SelectMany(activity => activity.Events)
            .Select(scheduled => scheduled.Id)
            .ToHashSet();
    }

    private async Task RefreshAsync(DateTimeOffset now)
    {
        // One read at a time; the next call after it finishes picks up anything newer.
        if (Interlocked.Exchange(ref _reading, 1) == 1)
        {
            return;
        }

        try
        {
            _readAt = now;
            List<AccountResetActivity> activities = await Task.Run(Read);
            _data = ResetProgressData.Empty with { Activities = activities };
        }
        catch (Exception)
        {
            // The database not ready yet (fresh install): try again next time.
        }
        finally
        {
            Volatile.Write(ref _reading, 0);
        }
    }

    private List<AccountResetActivity> Read()
    {
        string? accountId = string.IsNullOrEmpty(_account.AccountId) ? null : _account.AccountId;
        if (accountId is null)
        {
            return [];
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        if (!DatabaseSchema.Allows(scope))
        {
            return [];
        }

        var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        return db.AccountResetActivities.AsNoTracking()
            .Where(a => a.AccountId == accountId
                && (a.Kind == ResetActivityKind.WorldBoss || a.Kind == ResetActivityKind.MapChest))
            .OrderBy(a => a.Kind).ThenBy(a => a.Order)
            .ToList();
    }
}
