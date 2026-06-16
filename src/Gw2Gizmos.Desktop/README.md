# Gw2Gizmos (desktop app)

The Windows companion app — a [WPF](https://learn.microsoft.com/dotnet/desktop/wpf/) +
[WPF-UI](https://github.com/lepoco/wpfui) tray application (`net10.0-windows`) that presents Guild Wars 2
data ingested by the background worker. Builds `Gw2Gizmos.exe`.

> For end-user install and feature overview, see the [root README](../../README.md). This document is for
> developers working on the app.

## How it runs

On launch the app:

1. Builds a generic `Host` (DI + Serilog) and starts its background services.
2. **Spawns the worker** ([`Gw2Gizmos.Data.Worker.Cli.exe`](../Gw2Gizmos.Data.Worker.Cli)) as a child
   process bound to a job object, so the worker dies with the desktop.
3. Serves the worker its live config (keys, feature toggles, intervals, sync triggers, delete requests)
   over a per-run **named pipe** — the worker never reads the desktop's encrypted key file.
4. Opens the SQLite database **read-only** (the worker is the sole writer) for the UI's reads.
5. Runs the lightweight trading-post **delivery-notification** poller in-process (cheap enough to avoid
   cross-process plumbing).

Single instance per user (a mutex); closing the window hides to the tray.

## UI

A WPF-UI `NavigationView` shell. Pages live in `Views/`, view-models in `ViewModels/` (MVVM via
`Mvvm/ViewModelBase` + `RelayCommand`), wrapped in the shared `Controls/PageShell` (Win11-Settings-style
breadcrumb header over centered, width-capped content):

- **Dashboard** (startup page) — app/worker/account/session stat cards, loaded off the UI thread.
- **Account** → wallet, material storage, bank, shared inventory, characters → per-character inventory.
- **Sessions** → a sitting → its character-switch timeline → per-segment loot.
- **Events**, **Items**, **Notifications**, **Logs**, **API keys**.
- **Settings** → feature toggles, launch-at-startup, **Advanced** (intervals), **Stored data** (delete
  local data per type/account).

Read access to the worker-owned DB goes through `AccountReader` (read-only, off the UI thread).

## Per-user state

Stored as JSON files (and a DPAPI-encrypted key file) under `%APPDATA%\Gw2Gizmos` via `AppPaths`:

- `FileGw2ApiKeyStore` (API keys, DPAPI-encrypted), `FeatureSettingsStore`, `IntervalSettingsStore`.
- `SyncTriggerStore` / `DeleteRequestStore` — commands shipped to the worker over the pipe.
- `Selected{Account,Character,Session}Service` — current navigation context.
- `StartupRegistration` (launch-at-Windows-startup, HKCU Run), `UpdateStatus` (pending Velopack update).

## Updates & startup

Self-updates via [Velopack](https://github.com/velopack/velopack): the startup check downloads/stages new
releases, and the dashboard's App card offers **Restart now** (else it applies on the next restart). The
**Launch at Windows startup** setting registers the app (with `--minimized`) in the per-user Run key.

## Dev notes

- **Debug builds** use a separate AppData folder (`Gw2Gizmos.Dev`) and single-instance mutex, so a dev
  build can run alongside the installed release.
- Run from your IDE or `dotnet run --project src/Gw2Gizmos.Desktop`. Release packaging (Velopack installer)
  is done in CI — see [CONTRIBUTING.md](../../CONTRIBUTING.md).
