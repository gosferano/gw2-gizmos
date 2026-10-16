# Gw2Gizmos.Data.Worker

The data-ingestion engine — a library that syncs Guild Wars 2 data into the local SQLite database and is
the database's **sole writer**. It's hosted by [`Gw2Gizmos.Data.Worker.Cli`](../Gw2Gizmos.Data.Worker.Cli)
(the `Gw2Gizmos.Data.Worker.Cli.exe` the desktop spawns as a child process), and a lightweight slice of it
runs in-process in the [desktop app](../Gw2Gizmos.Desktop/README.md) for trading-post delivery alerts.

> Not a NuGet package — an internal library shared by the worker host and the desktop.

## What it syncs

Each kind of data runs on its own cadence (a `PeriodicTimer`, retimed live from the interval gate), with a
catalog of defaults in `Features/WorkerSyncs.cs`:

- **Item catalog**, **recipes**, **currencies**, **material categories** — reference data.
- **Trading-post prices** — appended as `PriceSnapshot` history; feeds the precomputed craft-cost cache.
- **Account data** — wallet, material storage, bank, shared inventory, and per-character inventories,
  recorded as an **append-on-change observation log** (so balances/holdings are reconstructable over time).
- **Play sessions** — detected from MumbleLink by `SessionTracker` (Windows-only), written as
  `GameSession` → `CharacterSegment` rows.

## How it's composed

```csharp
services.AddGw2GizmosIngestion(connectionString);          // the full engine (worker host)
services.AddGw2GizmosDeliveryNotifications(connectionString); // just the TP delivery poller (desktop)
serviceProvider.MigrateGw2GizmosDb();                      // bring the schema up to date, once at startup
```

`AddGw2GizmosIngestion` registers the `Worker` background service (the sync loops), the `SessionTracker`
(Windows), the `DeleteRequestProcessor`, and the scoped updaters/deleter.

## Configuration & gating

The worker reads its live config through small interfaces — `IGw2ApiKeyProvider`, `IFeatureGate`,
`IIntervalGate`, `ISyncTriggerSource`, `IDeleteRequestSource`:

- **Desktop-spawned:** an IPC provider fetches keys + feature toggles + intervals + commands from the
  desktop over a named pipe (the worker never reads the desktop's encrypted key file).
- **Standalone:** the same gates fall back to configuration (`Worker:Features:*`, `Worker:Intervals:*`,
  `GW2_API_KEY` / `Gw2:ApiKey`); the trigger/delete sources are no-ops.

Each sync section is gated by a `WorkerFeatures` entry and its required GW2 permissions; an enabled feature
whose key lacks a permission is logged (and surfaced in the desktop UI) rather than failing silently.
**Sync triggers** (monotonic generations) let the desktop ask for an immediate sync when the user enables a
feature or adds a key; **delete requests** let the desktop ask the worker to clear stored data (the worker
is the only writer). Both are serialized against the periodic account sync by `AccountSyncGate`.

## Database

The concrete engine is abstracted behind `IGw2GizmosDbProvider` (see
[`Gw2Gizmos.Data.Provider.Sqlite`](../Gw2Gizmos.Data.Provider.Sqlite)); this project stays provider-agnostic.
On startup the provider applies migrations and **self-heals** if the on-disk migration history has diverged
from the build (renames the old DB aside to a timestamped `*.bak` and rebuilds). The EF Core model lives in
[`Gw2Gizmos.Data.EntityFramework`](../Gw2Gizmos.Data.EntityFramework).

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for building and the migration workflow.
