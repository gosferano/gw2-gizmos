# Contributing to Gw2Gizmos

Thanks for your interest in contributing! This document covers building from source, the repository
layout, and how releases work. For what the project *is*, see the [README](README.md).

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download).
- The **desktop app** ([`Gw2Gizmos.Desktop`](src/Gw2Gizmos.Desktop/README.md)) builds and runs on Windows and
  Linux. The deprecated WPF app (`Gw2Gizmos.Desktop.Legacy`) needs Windows to run (it builds elsewhere with
  `-p:EnableWindowsTargeting=true`), as does the MumbleLink reader's shared-memory path.

## Building & running

```bash
dotnet build Gw2Gizmos.sln                       # build everything
dotnet run --project src/Gw2Gizmos.Desktop      # run the desktop app (Windows or Linux)
dotnet test                                       # run the test suite (tests/)
```

All projects target **.NET 10** and use [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management);
shared build settings live in the root `Directory.*.props`. Package versions are pinned in
`Directory.Packages.props` — add new dependencies there, not in individual `.csproj` files.

## Repository layout

```
Gw2Gizmos.sln
Directory.Build.props          # universal build settings (net10, nullable-as-error, implicit usings)
Directory.Packages.props       # central package management for the whole repo
src/
  Directory.Build.props        # + packaging metadata & SourceLink (shippable libs)
  Gw2Gizmos.Gw2Api.Contract/   # ── NuGet: GW2 API v2 DTOs                  → README
  Gw2Gizmos.Gw2Api.Client/     # ── NuGet: typed GW2 API v2 client          → README
  Gw2Gizmos.MumbleLink.Contract/ # internal: MumbleLink snapshot DTOs       → README
  Gw2Gizmos.MumbleLink.Client/   # internal: MumbleLink shared-memory reader → README
  Gw2Gizmos.MumbleLink.Bridge/   # win-x64 exe mirroring MumbleLink out of GW2's Wine/Proton prefix (Linux)
  Gw2Gizmos.Desktop/           # ── desktop app, Windows + Linux (builds Gw2Gizmos[.exe]) → README
  Gw2Gizmos.Desktop.Legacy/    # deprecated WPF desktop app (Windows)       → README
  Gw2Gizmos.Desktop.Core/      # internal: UI-agnostic view-models + services for the desktop app
  Gw2Gizmos.Desktop.Platform.{Windows,Linux}/  # internal: per-OS toasts, autostart, key storage, …
  Gw2Gizmos.Data.Worker/       # internal: data-ingestion engine (library)  → README
  Gw2Gizmos.Data.Worker.Cli/   # worker host (the exe the desktop spawns)
  Gw2Gizmos.Data.EntityFramework/  # EF Core model (DbContext + entities)
  Gw2Gizmos.Data.Provider.Sqlite/  # SQLite provider + migrations
  Gw2Gizmos.Data.Static/       # hardcoded event data (world bosses, metas)
  Gw2Gizmos.RecipeFinder[.Cli]/    # crafting-cost engine + export tool
  Gw2Gizmos.Sandbox/, Gw2Gizmos.Utilities.*   # dev scratch / utilities
tools/
  Gw2Gizmos.Gw2Api.NullabilityAudit/  # samples the live API to audit DTO nullability
tests/                          # xUnit test projects
scripts/
  add-migration.ps1, remove-migration.ps1     # EF migrations across provider projects
```

### Architecture in one paragraph

The **worker** ([`Gw2Gizmos.Data.Worker`](src/Gw2Gizmos.Data.Worker/README.md)) is the **sole writer** of
the SQLite database; the **desktop app** ([`Gw2Gizmos.Desktop`](src/Gw2Gizmos.Desktop/README.md)) opens it
**read-only** and spawns the worker as a child process, handing it live config (API keys, feature toggles,
intervals, commands) over a local named pipe. This single-writer split is deliberate — don't write to the DB
from the desktop. See those two READMEs for the details.

## Database migrations

Migrations are **provider-specific** and live in each `Gw2Gizmos.Data.Provider.*` project. Use the script
so every provider stays in sync:

```bash
./scripts/add-migration.ps1 <MigrationName>
```

The worker applies migrations on startup (`Migrate()`), and **self-heals** if the on-disk history has
diverged from the build's migrations (it renames the old DB aside to a timestamped `*.bak` and rebuilds).
`DateTimeOffset` is stored as UTC ticks on SQLite (a value converter in the SQLite provider) so dates can be
ordered/compared server-side.

## Tests

xUnit, under `tests/`. Run with `dotnet test`. Tests that need Windows-only APIs (e.g. the MumbleLink reader
against named shared memory) guard with `OperatingSystem.IsWindows()` and no-op elsewhere.

## Commits & pull requests

- Work on a feature branch and open a PR against `main`.
- Keep commits focused; write a clear message describing the *why*.
- Make sure `dotnet build` and `dotnet test` pass before pushing.

## Releases & versioning

Each component carries its own `<Version>` and is released by GitHub Actions on push to `main` **when that
version is new** — bumping a project's version is what triggers its release.

| Component | Tag | Published to |
| --- | --- | --- |
| Gw2Api Contract | `gw2-api-contract-<v>` | NuGet.org |
| Gw2Api Client | `gw2-api-client-<v>` | NuGet.org |
| Desktop | `gw2gizmos-desktop-<v>` | GitHub Release (Velopack: Windows installer + Linux AppImage) |

NuGet publishing uses [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
(OIDC, no API keys). Packages ship with SourceLink and `.snupkg` symbols for step-into debugging. The
desktop app's version is `<Version>` in `src/Gw2Gizmos.Desktop/Gw2Gizmos.Desktop.csproj`; its release is built
with [Velopack](https://github.com/velopack/velopack) — the Windows installer on a Windows runner, the Linux
AppImage on a Linux runner, both attached to one GitHub release with each platform's update feed — and
self-updates on the next restart. A hyphenated version (e.g. `0.7.0-beta.1`) is released as a GitHub
**prerelease**: installed apps update from the latest release only, so they don't pick it up, and testers install
it from its release page. The next stable version updates everyone.
