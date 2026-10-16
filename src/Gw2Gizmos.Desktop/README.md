# Gw2Gizmos (desktop app)

The cross-platform companion app: an [Avalonia](https://avaloniaui.net/) +
[FluentAvalonia](https://github.com/amwx/FluentAvalonia) tray application for **Windows and Linux** that presents
Guild Wars 2 data ingested by the background worker. Builds `Gw2Gizmos.exe` (Windows) / `Gw2Gizmos` (Linux). It
replaces the WPF app ([`Gw2Gizmos.Desktop.Legacy`](../Gw2Gizmos.Desktop.Legacy)), which is deprecated.

> For end-user install and feature overview, see the [root README](../../README.md). This document is for
> developers working on the app.

## How it runs

On launch the app:

1. Builds a generic `Host` (DI + Serilog) and starts its background services.
2. **Spawns the worker** ([`Gw2Gizmos.Data.Worker.Cli`](../Gw2Gizmos.Data.Worker.Cli)) as a child process and
   restarts it if it dies (`WorkerSupervisor`). The worker also watches the app's pid and exits with it; on
   Windows it's additionally bound to a kill-on-close job.
3. Serves the worker its live config (keys, feature toggles, intervals, sync triggers, delete requests) over a
   per-run local pipe — the worker never reads the app's encrypted key file.
4. Opens the SQLite database **read-only** (the worker is the sole writer); open pages refresh in place as the
   worker writes (`DataChangeMonitor`).

Single instance per user (a mutex); closing the window hides to the tray.

## Layout

- The view-models, stores and services live in the UI-agnostic
  [`Gw2Gizmos.Desktop.Core`](../Gw2Gizmos.Desktop.Core), shared with the WPF app. This project implements its UI
  seams (`Ui/`: dispatcher and timers, collection views, dialogs, clipboard; `Navigation/`) and holds the shell
  (`MainWindow`, tray), the pages (`Views/`), and shared controls (`Controls/`, incl. the bundled Fluent System
  Icons font and the ScottPlot price chart).
- Platform services come from a platform library, chosen at build time:
  - [`Gw2Gizmos.Desktop.Platform.Windows`](../Gw2Gizmos.Desktop.Platform.Windows) — WinRT toasts, HKCU Run
    autostart, DPAPI, the worker's kill-on-close job.
  - [`Gw2Gizmos.Desktop.Platform.Linux`](../Gw2Gizmos.Desktop.Platform.Linux) — `notify-send`, XDG autostart, the
    Secret Service keyring (`secret-tool`), and the Proton MumbleLink bridge launcher.

The target platform is the runtime identifier when one is given (`-r win-x64` / `-r linux-x64`, so either can be
cross-built), otherwise the OS building it. A Windows build targets `net10.0-windows10.0.17763.0` and compiles with
the `WINDOWS` symbol.

## Play sessions on Linux

GW2 runs under Steam's Proton there, so its MumbleLink shared memory lives inside Wine. While Play sessions is on,
the app watches for the game and starts the bundled bridge
([`Gw2Gizmos.MumbleLink.Bridge`](../Gw2Gizmos.MumbleLink.Bridge), a win-x64 exe shipped in `mumblelink-bridge/`) in
the game's own Wine session; it creates the mapping, mirrors it into `MumbleLink` in the data folder, and exits after
the game does. Nothing needs configuring in Steam.

## Per-user state

`%APPDATA%\Gw2Gizmos` on Windows (shared with the WPF app), `$XDG_DATA_HOME/Gw2Gizmos` (usually
`~/.local/share`, owner-only) on Linux: the database, settings JSON, the encrypted key file, logs and the icon cache.
**Debug builds** use `Gw2Gizmos.Dev` (and their own single-instance mutex), so a dev build runs alongside an
installed release.

## Building & packaging

Run with `dotnet run --project src/Gw2Gizmos.Desktop`. A build copies the worker next to the app (and, on Linux,
publishes the bridge into `mumblelink-bridge/`); `dotnet publish -r <rid> --self-contained` merges the worker's
publish into the app's. Releases are packed with [Velopack](https://github.com/velopack/velopack) in CI — a Windows
installer and a Linux AppImage in the same GitHub release — see [CONTRIBUTING.md](../../CONTRIBUTING.md).
