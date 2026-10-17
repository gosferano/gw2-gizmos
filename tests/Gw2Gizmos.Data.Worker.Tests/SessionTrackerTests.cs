using System.Diagnostics.CodeAnalysis;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Updaters;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.MumbleLink.Client;
using Gw2Gizmos.MumbleLink.Contract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Integration tests for <see cref="SessionTracker"/>: drives the state machine one <c>TickAsync</c> at a time with
/// a scripted MumbleLink reader and a controllable clock (instead of racing the real PeriodicTimer), and asserts the
/// GameSession / CharacterSegment rows it writes through launch → switch → char-select → close. The boundary
/// account-sync is made a no-op via an empty API-key provider, so these tests don't need the HTTP stub.
/// </summary>
public sealed class SessionTrackerTests : IDisposable
{
    private const string AccountId = "A.1111";

    private readonly WorkerDbFixture _fixture = new();
    private readonly FakeMumbleLinkReader _reader = new();
    private readonly MutableTimeProvider _time = new();
    private readonly ServiceProvider _provider;
    private readonly SessionTracker _tracker;

    private uint _tick;

    // No keys (so a sync makes no requests), but it can be made to "take" time, like a real sync.
    private readonly SlowSyncKeyProvider _keys;

    public SessionTrackerTests()
    {
        _keys = new SlowSyncKeyProvider(_time);
        SeedAccount();

        // A real scope factory over the same temp DB, plus a no-op AccountSyncUpdater (no keys → SyncAccount returns
        // before any HTTP). This mirrors the worker's scoped-per-boundary DbContext usage.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<Gw2GizmosDbContext>(o => new SqliteDbProvider().Configure(o, _fixture.ConnectionString));
        services.AddSingleton<TimeProvider>(_time);
        services.AddSingleton<IGw2ApiKeyProvider>(_keys);
        services.AddSingleton<IFeatureGate, AllEnabledFeatureGate>();
        services.AddSingleton<IGw2ApiClientFactory>(new RoutingHttpStub().BuildClientFactory()); // never called (no keys)
        services.AddScoped<AccountSyncUpdater>();
        _provider = services.BuildServiceProvider();

        _tracker = new SessionTracker(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _reader,
            new AllEnabledFeatureGate(),
            new AccountSyncGate(),
            _time,
            NullLogger<SessionTracker>.Instance
        )
        {
            TickProbeInterval = TimeSpan.Zero,
            // ProcessId 0 means "can't tell" — not whatever happens to be running on this machine.
            GameProcessAlive = _ => null,
        };
    }

    public void Dispose()
    {
        _provider.Dispose();
        _fixture.Dispose();
    }

    [Fact]
    public async Task Launch_WithCharacter_OpensSessionAndFirstSegment()
    {
        await Live("Alice");

        GameSession session = await Db.GameSessions.SingleAsync();
        Assert.Equal(AccountId, session.AccountId);
        Assert.Null(session.EndedAtUtc); // still open

        CharacterSegment segment = await Db.CharacterSegments.SingleAsync();
        Assert.Equal(0, segment.Sequence);
        Assert.Equal("Alice", segment.CharacterName);
        Assert.Null(segment.EndedAtUtc);
    }

    [Fact]
    public async Task CharSelectBeforeAnyCharacter_DoesNotOpenSession()
    {
        // Game is live but sitting at character select (no identity) — no session until a character loads.
        await Live(character: null);

        Assert.Equal(0, await Db.GameSessions.CountAsync());
        Assert.Equal(0, await Db.CharacterSegments.CountAsync());
    }

    [Fact]
    public async Task SwitchCharacter_ClosesSegmentAndOpensNext()
    {
        await Live("Alice");
        await Live("Bob"); // direct switch, no char-select gap

        Assert.Equal(1, await Db.GameSessions.CountAsync()); // same sitting

        List<CharacterSegment> segments = await OrderedSegments();
        Assert.Equal(2, segments.Count);

        Assert.Equal("Alice", segments[0].CharacterName);
        Assert.Equal(0, segments[0].Sequence);
        Assert.NotNull(segments[0].EndedAtUtc); // closed at the switch

        Assert.Equal("Bob", segments[1].CharacterName);
        Assert.Equal(1, segments[1].Sequence);
        Assert.Null(segments[1].EndedAtUtc); // now active
    }

    [Fact]
    public async Task CharSelect_ClosesSegment_ThenReturnOpensNew()
    {
        await Live("Alice");
        await Live(character: null); // back to character select
        List<CharacterSegment> afterSelect = await OrderedSegments();
        CharacterSegment alice = Assert.Single(afterSelect);
        Assert.NotNull(alice.EndedAtUtc); // segment closed; nothing open while at select

        await Live("Alice"); // returned to a character — a fresh segment opens
        List<CharacterSegment> afterReturn = await OrderedSegments();
        Assert.Equal(2, afterReturn.Count);
        Assert.Equal(1, afterReturn[1].Sequence);
        Assert.Null(afterReturn[1].EndedAtUtc);

        Assert.Equal(1, await Db.GameSessions.CountAsync()); // still one sitting
    }

    [Fact]
    public async Task GameClose_EndsSessionAndOpenSegment()
    {
        await Live("Alice");

        // Activity recorded mid-session, so it isn't discarded as empty on end.
        _time.Advance(TimeSpan.FromMinutes(10));
        SeedObservation(_time.Now);

        // Game process gone: the reader returns no block at all → session and its open segment close.
        _time.Advance(TimeSpan.FromMinutes(20));
        _reader.Snapshot = null;
        await _tracker.TickAsync(CancellationToken.None);

        GameSession session = await Db.GameSessions.SingleAsync();
        Assert.NotNull(session.EndedAtUtc);
        CharacterSegment segment = await Db.CharacterSegments.SingleAsync();
        Assert.NotNull(segment.EndedAtUtc);
    }

    [Fact]
    public async Task GameClose_WithNoActivity_DiscardsEmptySession()
    {
        await Live("Alice"); // no observations recorded during the sitting

        _time.Advance(TimeSpan.FromMinutes(30));
        _reader.Snapshot = null;
        await _tracker.TickAsync(CancellationToken.None);

        // An empty session (and its segments) is dropped rather than kept.
        Assert.Equal(0, await Db.GameSessions.CountAsync());
        Assert.Equal(0, await Db.CharacterSegments.CountAsync());
    }

    [Fact]
    public async Task TickFreeze_EndsSession_WhenProcessIdUnknown()
    {
        await Live("Alice"); // opens with a tick; ProcessId is 0 (unknown) so the freeze timeout is the fallback
        await Live("Alice"); // played a little
        DateTimeOffset lastMoved = _time.Now;

        // Activity recorded so the session survives the end (not discarded as empty) — by a sync that ran while the
        // game was already quiet, so it's stamped after the tick stopped.
        _time.Advance(TimeSpan.FromSeconds(30));
        SeedObservation(_time.Now);

        // Same UiTick on the next read (frozen), and time advances past the 60s fallback → game-end inferred.
        _time.Advance(TimeSpan.FromSeconds(60));
        _reader.Snapshot = Snapshot(_tick, "Alice"); // note: NOT advancing _tick → UiTick unchanged
        await _tracker.TickAsync(CancellationToken.None);

        GameSession session = await Db.GameSessions.SingleAsync();
        Assert.Equal(lastMoved, session.EndedAtUtc);
        // The quiet-time sync's observation is dated to the end, so it counts towards the session.
        Assert.Equal(lastMoved, (await Db.AccountItemObservations.AsNoTracking().SingleAsync()).ObservedAtUtc);
    }

    // --- maps ---

    [Fact]
    public async Task MapChanges_SplitTheCharacterSegmentByMap()
    {
        await Live("Alice", mapId: 50); // opens the session and segment
        await Live("Alice", mapId: 50); // first map
        await Live("Alice", mapId: 1422); // travelled
        await Live("Alice", mapId: 1422); // same map: nothing new

        long segmentId = (await Db.CharacterSegments.SingleAsync()).Id;
        List<MapSegment> maps = await MapSegments();
        Assert.Equal([50, 1422], maps.Select(m => m.MapId));
        Assert.All(maps, m => Assert.Equal(segmentId, m.CharacterSegmentId));
        Assert.Equal(maps[1].StartedAtUtc, maps[0].EndedAtUtc); // left one as entering the next
        Assert.Null(maps[1].EndedAtUtc);
    }

    [Fact]
    public async Task UnknownMap_IsNotRecorded()
    {
        await Live("Alice", mapId: 0);
        await Live("Alice", mapId: 0);

        Assert.Empty(await MapSegments());
    }

    [Fact]
    public async Task SwitchCharacter_ClosesTheMapSegment_AndTheNextCharacterGetsItsOwn()
    {
        await Live("Alice", mapId: 50);
        await Live("Alice", mapId: 50);
        await Live("Bob", mapId: 50); // switch: Alice's map closes with her segment
        await Live("Bob", mapId: 50); // Bob's first map

        List<CharacterSegment> segments = await OrderedSegments();
        List<MapSegment> maps = await MapSegments();
        Assert.Equal(2, maps.Count);
        Assert.Equal(segments[0].Id, maps[0].CharacterSegmentId);
        Assert.Equal(segments[0].EndedAtUtc, maps[0].EndedAtUtc);
        Assert.Equal(segments[1].Id, maps[1].CharacterSegmentId);
        Assert.Null(maps[1].EndedAtUtc);
    }

    [Fact]
    public async Task GameClose_ClosesTheOpenMapSegment()
    {
        await Live("Alice", mapId: 50);
        await Live("Alice", mapId: 50);
        _time.Advance(TimeSpan.FromMinutes(10));
        SeedObservation(_time.Now);

        _time.Advance(TimeSpan.FromMinutes(20));
        _reader.Snapshot = null;
        await _tracker.TickAsync(CancellationToken.None);

        MapSegment map = Assert.Single(await MapSegments());
        Assert.Equal((await Db.GameSessions.AsNoTracking().SingleAsync()).EndedAtUtc, map.EndedAtUtc);
    }

    [Fact]
    public async Task DiscardingAnEmptySession_DeletesItsMapSegments()
    {
        await Live("Alice", mapId: 50);
        await Live("Alice", mapId: 50); // no observations recorded during the sitting

        _time.Advance(TimeSpan.FromMinutes(30));
        _reader.Snapshot = null;
        await _tracker.TickAsync(CancellationToken.None);

        Assert.Equal(0, await Db.GameSessions.CountAsync());
        Assert.Empty(await MapSegments());
    }

    [Fact]
    public async Task OnStartup_ASessionLeftOpen_HasItsMapSegmentClosedToo()
    {
        // A previous run left a session open on a map, then the worker stopped (the game isn't running now).
        long segmentId;
        using (Gw2GizmosDbContext db = _fixture.NewContext())
        {
            var session = new GameSession { AccountId = AccountId, StartedAtUtc = _time.Now };
            db.GameSessions.Add(session);
            db.SaveChanges();
            var segment = new CharacterSegment { GameSessionId = session.Id, CharacterName = "Alice", StartedAtUtc = _time.Now };
            db.CharacterSegments.Add(segment);
            db.SaveChanges();
            segmentId = segment.Id;
            db.MapSegments.Add(new MapSegment { CharacterSegmentId = segmentId, MapId = 50, StartedAtUtc = _time.Now });
            db.SaveChanges();
        }

        _time.Advance(TimeSpan.FromMinutes(10));
        SeedObservation(_time.Now);
        _reader.Snapshot = null;
        await _tracker.ResumeOrCloseOpenSessionsAsync(CancellationToken.None);

        MapSegment map = Assert.Single(await MapSegments());
        Assert.NotNull(map.EndedAtUtc);
        Assert.Equal((await Db.CharacterSegments.AsNoTracking().SingleAsync()).EndedAtUtc, map.EndedAtUtc);
    }

    [Fact]
    public async Task OnStartup_WhenTheProcessCantBeChecked_AnAdvancingTickResumesTheOpenSession()
    {
        // As under Wine/Proton: ProcessId 0 (can't be checked), but the game's tick is moving.
        long sessionId = SeedOpenSession("Alice", mapId: 50);
        _reader.Snapshot = Snapshot(_tick, "Alice", mapId: 50);
        _reader.AdvanceOnRead = true;

        await _tracker.ResumeOrCloseOpenSessionsAsync(CancellationToken.None);

        GameSession session = await Db.GameSessions.AsNoTracking().SingleAsync();
        Assert.Equal(sessionId, session.Id);
        Assert.Null(session.EndedAtUtc);
        Assert.Null((await Db.CharacterSegments.AsNoTracking().SingleAsync()).EndedAtUtc);

        // The next tick carries on in the same session and segment (and reopens the map segment).
        await Live("Alice", mapId: 50);
        Assert.Equal(1, await Db.GameSessions.CountAsync());
        Assert.Single(await OrderedSegments());
    }

    [Fact]
    public async Task OnStartup_WhenTheProcessCantBeChecked_AFrozenTickClosesTheOpenSession()
    {
        SeedOpenSession("Alice", mapId: 50);
        _time.Advance(TimeSpan.FromMinutes(10));
        SeedObservation(_time.Now);
        _reader.Snapshot = Snapshot(_tick, "Alice", mapId: 50); // the block is there, but the tick doesn't move

        await _tracker.ResumeOrCloseOpenSessionsAsync(CancellationToken.None);

        Assert.NotNull((await Db.GameSessions.AsNoTracking().SingleAsync()).EndedAtUtc);
    }

    [Fact]
    public async Task A_session_ended_by_a_frozen_tick_ends_when_the_tick_last_moved()
    {
        await Live("Alice");
        SeedObservation(_time.Now.AddSeconds(1)); // some activity, so the session is kept
        await Live("Alice");
        DateTimeOffset lastMoved = _time.Now;

        // The game goes quiet (process can't be checked): the tick stops moving until the timeout runs out.
        for (int i = 0; i < 15; i++)
        {
            _time.Advance(TimeSpan.FromSeconds(5));
            _reader.Snapshot = Snapshot(_tick, "Alice");
            await _tracker.TickAsync(CancellationToken.None);
        }

        GameSession session = await Db.GameSessions.AsNoTracking().SingleAsync();
        Assert.Equal(lastMoved, session.EndedAtUtc);
        Assert.Equal(lastMoved, (await OrderedSegments()).Single().EndedAtUtc);
    }

    // --- out of the world (character select, minimised, long loading) ---

    /// <summary>The game keeps running but stops updating MumbleLink for <paramref name="duration"/>: the tick
    /// doesn't move.</summary>
    private async Task Quiet(TimeSpan duration, string character = "Alice", int mapId = 50)
    {
        for (TimeSpan passed = TimeSpan.Zero; passed < duration; passed += TimeSpan.FromSeconds(5))
        {
            _time.Advance(TimeSpan.FromSeconds(5));
            _reader.Snapshot = Snapshot(_tick, character, mapId);
            await _tracker.TickAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Away_and_back_on_the_same_character_continues_the_segment_minus_the_time_away()
    {
        _tracker.GameProcessAlive = _ => true;
        await Live("Alice", mapId: 50);
        await Live("Alice", mapId: 50);
        DateTimeOffset left = _time.Now;

        await Quiet(TimeSpan.FromMinutes(3));
        CharacterSegment away = (await OrderedSegments()).Single();
        Assert.Equal(left, away.AwaySinceUtc);
        Assert.Equal(left, (await MapSegments()).Single().EndedAtUtc); // the map segment closed when it left

        await Live("Alice", mapId: 50);
        DateTimeOffset back = _time.Now;

        CharacterSegment segment = (await OrderedSegments()).Single();
        Assert.Null(segment.EndedAtUtc);
        Assert.Null(segment.AwaySinceUtc);
        Assert.Equal(back - left, segment.AwayTime);
        Assert.Equal(TimeSpan.FromSeconds(5), segment.InWorldTime(back)); // only the time in the world
        Assert.Equal(2, (await MapSegments()).Count); // a new map segment once back
        Assert.Null((await Db.GameSessions.AsNoTracking().SingleAsync()).EndedAtUtc);
    }

    [Fact]
    public async Task Away_and_back_on_another_character_ends_the_segment_when_it_left()
    {
        _tracker.GameProcessAlive = _ => true;
        await Live("Alice");
        await Live("Alice");
        DateTimeOffset left = _time.Now;

        await Quiet(TimeSpan.FromMinutes(2));
        await Live("Bob");
        DateTimeOffset back = _time.Now;

        List<CharacterSegment> segments = await OrderedSegments();
        Assert.Equal(("Alice", left), (segments[0].CharacterName, segments[0].EndedAtUtc!.Value));
        Assert.Null(segments[0].AwaySinceUtc);
        Assert.Equal(("Bob", back), (segments[1].CharacterName, segments[1].StartedAtUtc));
        Assert.Equal(1, await Db.GameSessions.CountAsync());
    }

    [Fact]
    public async Task Closing_the_game_while_away_ends_the_session_when_the_player_left_the_world()
    {
        bool running = true;
        _tracker.GameProcessAlive = _ => running;
        await Live("Alice");
        SeedObservation(_time.Now.AddSeconds(1));
        await Live("Alice");
        DateTimeOffset left = _time.Now;

        await Quiet(TimeSpan.FromMinutes(2));
        running = false;
        await Quiet(TimeSpan.FromSeconds(5));

        GameSession session = await Db.GameSessions.AsNoTracking().SingleAsync();
        Assert.Equal(left, session.EndedAtUtc);
        CharacterSegment segment = (await OrderedSegments()).Single();
        Assert.Equal(left, segment.EndedAtUtc);
        Assert.Null(segment.AwaySinceUtc);
    }

    [Fact]
    public async Task A_short_freeze_like_a_loading_screen_isnt_time_away()
    {
        _tracker.GameProcessAlive = _ => true;
        await Live("Alice", mapId: 50);
        await Live("Alice", mapId: 50);

        await Quiet(TimeSpan.FromSeconds(20));
        await Live("Alice", mapId: 50);

        CharacterSegment segment = (await OrderedSegments()).Single();
        Assert.Equal(TimeSpan.Zero, segment.AwayTime);
        Assert.Null(segment.AwaySinceUtc);
        Assert.Single(await MapSegments());
    }

    /// <summary>A session left open by a previous run, with an open character and map segment.</summary>
    private long SeedOpenSession(string character, int mapId)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        var session = new GameSession { AccountId = AccountId, StartedAtUtc = _time.Now };
        db.GameSessions.Add(session);
        db.SaveChanges();
        var segment = new CharacterSegment { GameSessionId = session.Id, CharacterName = character, StartedAtUtc = _time.Now };
        db.CharacterSegments.Add(segment);
        db.SaveChanges();
        db.MapSegments.Add(new MapSegment { CharacterSegmentId = segment.Id, MapId = mapId, StartedAtUtc = _time.Now });
        db.SaveChanges();
        return session.Id;
    }

    [Fact]
    public async Task Boundaries_are_stamped_when_they_happen_not_when_their_sync_finishes()
    {
        _keys.SyncTakes = TimeSpan.FromSeconds(90);

        _time.Advance(TimeSpan.FromSeconds(5));
        DateTimeOffset launched = _time.Now;
        _reader.Snapshot = Snapshot(++_tick, "Alice");
        await _tracker.TickAsync(CancellationToken.None);

        _time.Advance(TimeSpan.FromSeconds(5));
        DateTimeOffset switched = _time.Now;
        _reader.Snapshot = Snapshot(++_tick, "Bob");
        await _tracker.TickAsync(CancellationToken.None);

        Assert.Equal(launched, (await Db.GameSessions.AsNoTracking().SingleAsync()).StartedAtUtc);
        List<CharacterSegment> segments = await OrderedSegments();
        Assert.Equal(launched, segments[0].StartedAtUtc);
        Assert.Equal(switched, segments[0].EndedAtUtc);
        Assert.Equal(switched, segments[1].StartedAtUtc);
    }

    // --- driving helpers ---

    /// <summary>Advances the clock and feeds one "game live" tick with a fresh UiTick (so it's never seen as frozen)
    /// and the given active character (null = character-select screen), then runs a single TickAsync.</summary>
    private async Task Live(string? character, int mapId = 0)
    {
        _time.Advance(TimeSpan.FromSeconds(5));
        _tick++;
        _reader.Snapshot = Snapshot(_tick, character, mapId);
        await _tracker.TickAsync(CancellationToken.None);
    }

    /// <summary>A live snapshot: advancing UiTick, optional identity, the map (0 = unknown), ProcessId 0 (unknown →
    /// freeze-timeout fallback).</summary>
    private static MumbleLinkSnapshot Snapshot(uint uiTick, string? character, int mapId = 0) =>
        new()
        {
            UiTick = uiTick,
            Identity = character is null ? null : new MumbleIdentity { Name = character, MapId = mapId },
            Context = new MumbleContextInfo { ProcessId = 0, MapId = (uint)mapId },
        };

    private async Task<List<MapSegment>> MapSegments() =>
        (await Db.MapSegments.AsNoTracking().ToListAsync()).OrderBy(m => m.Id).ToList();

    private Gw2GizmosDbContext Db => _fixture.Db;

    private async Task<List<CharacterSegment>> OrderedSegments() =>
        (await Db.CharacterSegments.AsNoTracking().ToListAsync()).OrderBy(s => s.Sequence).ToList();

    private void SeedAccount()
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        db.Accounts.Add(new Account { AccountId = AccountId, Name = AccountId, World = 1001, LastSyncedUtc = _time.Now });
        // Roster so the tracker resolves the character→account link (it also has a sole-account fallback).
        db.Characters.Add(new Character { AccountId = AccountId, Name = "Alice" });
        db.Characters.Add(new Character { AccountId = AccountId, Name = "Bob" });
        db.SaveChanges();
    }

    /// <summary>Records one item observation at <paramref name="at"/>, marking real activity in a session's window
    /// (so the tracker doesn't discard the session as empty when it ends).</summary>
    private void SeedObservation(DateTimeOffset at)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        db.AccountItemObservations.Add(new AccountItemObservation
        {
            AccountId = AccountId,
            Container = AccountContainer.Bank,
            ItemId = 1,
            Count = 1,
            ObservedAtUtc = at,
        });
        db.SaveChanges();
    }

    /// <summary>A scriptable <see cref="IMumbleLinkReader"/>: <see cref="Read"/> returns whatever the test last set,
    /// or, with <see cref="AdvanceOnRead"/>, the same block with its tick moved on each read (a running game).</summary>
    private sealed class FakeMumbleLinkReader : IMumbleLinkReader
    {
        public MumbleLinkSnapshot? Snapshot { get; set; }

        public bool AdvanceOnRead { get; set; }

        public MumbleLinkSnapshot? Read()
        {
            if (AdvanceOnRead && Snapshot is not null)
            {
                Snapshot = new MumbleLinkSnapshot
                {
                    UiTick = Snapshot.UiTick + 1,
                    Identity = Snapshot.Identity,
                    Context = Snapshot.Context,
                };
            }

            return Snapshot;
        }

        public bool TryRead([NotNullWhen(true)] out MumbleLinkSnapshot? snapshot)
        {
            snapshot = Snapshot;
            return snapshot is not null;
        }

        public void Dispose() { }
    }

    /// <summary>No API keys, but each sync moves the clock on by <see cref="SyncTakes"/>, as a real sync takes time.</summary>
    private sealed class SlowSyncKeyProvider(MutableTimeProvider time) : IGw2ApiKeyProvider
    {
        public TimeSpan SyncTakes { get; set; }

        public string? GetApiKey() => null;

        public IReadOnlyList<string> GetApiKeys()
        {
            time.Advance(SyncTakes);
            return Array.Empty<string>();
        }
    }
}
