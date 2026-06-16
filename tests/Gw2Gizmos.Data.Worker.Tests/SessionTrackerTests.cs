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

    public SessionTrackerTests()
    {
        SeedAccount();

        // A real scope factory over the same temp DB, plus a no-op AccountSyncUpdater (no keys → SyncAccount returns
        // before any HTTP). This mirrors the worker's scoped-per-boundary DbContext usage.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<Gw2GizmosDbContext>(o => new SqliteDbProvider().Configure(o, _fixture.ConnectionString));
        services.AddSingleton<TimeProvider>(_time);
        services.AddSingleton<IGw2ApiKeyProvider, EmptyApiKeyProvider>();
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
        );
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

        // Game process gone: the reader returns no block at all → session and its open segment close.
        _time.Advance(TimeSpan.FromMinutes(30));
        _reader.Snapshot = null;
        await _tracker.TickAsync(CancellationToken.None);

        GameSession session = await Db.GameSessions.SingleAsync();
        Assert.NotNull(session.EndedAtUtc);
        CharacterSegment segment = await Db.CharacterSegments.SingleAsync();
        Assert.NotNull(segment.EndedAtUtc);
    }

    [Fact]
    public async Task TickFreeze_EndsSession_WhenProcessIdUnknown()
    {
        await Live("Alice"); // opens with a tick; ProcessId is 0 (unknown) so the freeze timeout is the fallback

        // Same UiTick on the next reads (frozen), and time advances past the 60s fallback → game-end inferred.
        _time.Advance(TimeSpan.FromSeconds(90));
        _reader.Snapshot = Snapshot(_tick, "Alice"); // note: NOT advancing _tick → UiTick unchanged
        await _tracker.TickAsync(CancellationToken.None);

        GameSession session = await Db.GameSessions.SingleAsync();
        Assert.NotNull(session.EndedAtUtc);
    }

    // --- driving helpers ---

    /// <summary>Advances the clock and feeds one "game live" tick with a fresh UiTick (so it's never seen as frozen)
    /// and the given active character (null = character-select screen), then runs a single TickAsync.</summary>
    private async Task Live(string? character)
    {
        _time.Advance(TimeSpan.FromSeconds(5));
        _tick++;
        _reader.Snapshot = Snapshot(_tick, character);
        await _tracker.TickAsync(CancellationToken.None);
    }

    /// <summary>A live snapshot: advancing UiTick, optional identity, ProcessId 0 (unknown → freeze-timeout fallback).</summary>
    private static MumbleLinkSnapshot Snapshot(uint uiTick, string? character) =>
        new()
        {
            UiTick = uiTick,
            Identity = character is null ? null : new MumbleIdentity { Name = character },
            Context = new MumbleContextInfo { ProcessId = 0 },
        };

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

    /// <summary>A scriptable <see cref="IMumbleLinkReader"/>: <see cref="Read"/> returns whatever the test last set.</summary>
    private sealed class FakeMumbleLinkReader : IMumbleLinkReader
    {
        public MumbleLinkSnapshot? Snapshot { get; set; }

        public MumbleLinkSnapshot? Read() => Snapshot;

        public bool TryRead([NotNullWhen(true)] out MumbleLinkSnapshot? snapshot)
        {
            snapshot = Snapshot;
            return snapshot is not null;
        }

        public void Dispose() { }
    }
}
