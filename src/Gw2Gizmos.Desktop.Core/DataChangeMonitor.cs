using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Desktop.Core.Ui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>What changed in the worker-owned data since the last notification.</summary>
[Flags]
public enum DataChanges
{
    None = 0,

    /// <summary>
    /// An account sync ran (its "synced …" time moved, as it starts) or its achievements and reset progress landed (at
    /// its end), whether or not anything changed.
    /// </summary>
    AccountSync = 1,

    /// <summary>Wallet or item holdings changed (new observations) — balances and session loot.</summary>
    Holdings = 2,

    /// <summary>A play session or character segment started, ended, or was deleted, or the map changed.</summary>
    Sessions = 4,

    /// <summary>The public catalog grew: items, recipes, currencies, or a price poll.</summary>
    Catalog = 8,

    /// <summary>A minute passed — refresh relative times ("5 min ago") and running durations.</summary>
    Clock = 16,

    /// <summary>The account's value was recorded (a new point, or this hour's updated).</summary>
    AccountValue = 32,
}

/// <summary>
/// Tells open pages when the worker has written something they show, so they refresh in place instead of only on
/// navigation. The worker is the database's only writer and doesn't signal the UI, so this polls a cheap stamp of
/// the data (a few indexed counts and max ids) every few seconds — only while a page is subscribed — and raises the
/// differences on the UI thread.
/// </summary>
public sealed class DataChangeMonitor
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ClockInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUiDispatcher _dispatcher;
    private readonly object _gate = new();
    private readonly List<Action<DataChanges>> _handlers = new();
    private Timer? _timer;
    private DataStamp? _last;
    private DateTimeOffset _lastClock;
    private int _polling;

    public DataChangeMonitor(IServiceScopeFactory scopeFactory, IUiDispatcher dispatcher)
    {
        _scopeFactory = scopeFactory;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Calls <paramref name="handler"/> on the UI thread whenever the data changes; dispose the result to stop.
    /// </summary>
    public IDisposable Subscribe(Action<DataChanges> handler)
    {
        lock (_gate)
        {
            _handlers.Add(handler);
            if (_timer is null)
            {
                // Fresh baseline: the subscribing page has just loaded, so only changes from here on matter.
                _last = null;
                _lastClock = DateTimeOffset.UtcNow;
                _timer = new Timer(_ => Poll(), null, TimeSpan.Zero, PollInterval);
            }
        }

        return new Subscription(this, handler);
    }

    private void Unsubscribe(Action<DataChanges> handler)
    {
        lock (_gate)
        {
            _handlers.Remove(handler);
            if (_handlers.Count == 0)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }
    }

    private void Poll()
    {
        // A slow read (cold DB) mustn't stack polls.
        if (Interlocked.Exchange(ref _polling, 1) == 1)
        {
            return;
        }

        try
        {
            DataStamp? stamp = ReadStamp();
            DataChanges changes = DataChanges.None;
            lock (_gate)
            {
                if (stamp is not null)
                {
                    if (_last is { } last)
                    {
                        changes = stamp.Compare(last);
                    }

                    _last = stamp;
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (now - _lastClock >= ClockInterval)
                {
                    _lastClock = now;
                    changes |= DataChanges.Clock;
                }

                if (changes == DataChanges.None || _handlers.Count == 0)
                {
                    return;
                }
            }

            _dispatcher.Post(() =>
            {
                Action<DataChanges>[] handlers;
                lock (_gate)
                {
                    handlers = _handlers.ToArray();
                }

                foreach (Action<DataChanges> handler in handlers)
                {
                    handler(changes);
                }
            });
        }
        finally
        {
            Volatile.Write(ref _polling, 0);
        }
    }

    private DataStamp? ReadStamp()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            if (!DatabaseSchema.Allows(scope))
            {
                return null; // Not migrated yet: try again next poll.
            }

            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            // A handful of accounts at most; the max is taken here so it doesn't depend on the provider's date support.
            DateTimeOffset lastSync = db.Accounts.AsNoTracking().Select(a => a.LastSyncedUtc).AsEnumerable()
                .DefaultIfEmpty().Max();
            // The account's "synced" time is stamped as a sync starts, but achievements and the reset's progress are
            // synced last, so their own sync times tell when those land.
            DateTimeOffset lastProgress = new[]
            {
                db.AccountAchievements.AsNoTracking().OrderByDescending(a => a.SyncedAtUtc)
                    .Select(a => a.SyncedAtUtc).FirstOrDefault(),
                db.AccountResetActivities.AsNoTracking().OrderByDescending(a => a.SyncedAtUtc)
                    .Select(a => a.SyncedAtUtc).FirstOrDefault(),
                db.AccountWizardsVaultMetas.AsNoTracking().OrderByDescending(m => m.SyncedAtUtc)
                    .Select(m => m.SyncedAtUtc).FirstOrDefault(),
            }.Max();
            // The newest point is re-stamped when a sync in the same hour updates it, so its time changes too.
            var lastValue = db.AccountValueSnapshots.AsNoTracking()
                .OrderByDescending(s => s.Id)
                .Select(s => new { s.Id, s.TakenAtUtc })
                .FirstOrDefault();
            return new DataStamp(
                lastSync,
                lastProgress,
                db.AccountWalletObservations.Max(o => (long?)o.Id) ?? 0,
                db.AccountItemObservations.Max(o => (long?)o.Id) ?? 0,
                db.GameSessions.Count(),
                db.GameSessions.Count(s => s.EndedAtUtc != null),
                db.CharacterSegments.Count(),
                db.CharacterSegments.Count(s => s.EndedAtUtc != null),
                db.MapSegments.Count(),
                db.Items.Count(),
                db.Recipes.Count(),
                db.Currencies.Count(),
                db.PriceSnapshots.Max(p => (long?)p.Id) ?? 0,
                (lastValue?.Id ?? 0, lastValue?.TakenAtUtc ?? default));
        }
        catch (Exception)
        {
            // Database not ready (fresh install) or busy — try again next poll.
            return null;
        }
    }

    private sealed record DataStamp(
        DateTimeOffset LastSync,
        DateTimeOffset LastProgressSync,
        long LastWalletObservation,
        long LastItemObservation,
        int Sessions,
        int EndedSessions,
        int Segments,
        int EndedSegments,
        int MapSegments,
        int Items,
        int Recipes,
        int Currencies,
        long LastPriceSnapshot,
        (long Id, DateTimeOffset TakenAtUtc) LastValuePoint)
    {
        public DataChanges Compare(DataStamp before)
        {
            DataChanges changes = DataChanges.None;
            if (LastSync != before.LastSync || LastProgressSync != before.LastProgressSync)
            {
                changes |= DataChanges.AccountSync;
            }

            if (LastWalletObservation != before.LastWalletObservation || LastItemObservation != before.LastItemObservation)
            {
                changes |= DataChanges.Holdings;
            }

            if ((Sessions, EndedSessions, Segments, EndedSegments, MapSegments)
                != (before.Sessions, before.EndedSessions, before.Segments, before.EndedSegments, before.MapSegments))
            {
                changes |= DataChanges.Sessions;
            }

            if ((Items, Recipes, Currencies, LastPriceSnapshot)
                != (before.Items, before.Recipes, before.Currencies, before.LastPriceSnapshot))
            {
                changes |= DataChanges.Catalog;
            }

            if (LastValuePoint != before.LastValuePoint)
            {
                changes |= DataChanges.AccountValue;
            }

            return changes;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private DataChangeMonitor? _owner;
        private readonly Action<DataChanges> _handler;

        public Subscription(DataChangeMonitor owner, Action<DataChanges> handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.Unsubscribe(_handler);
        }
    }
}
