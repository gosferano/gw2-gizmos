# Gw2Gizmos

A suite of [Guild Wars 2](https://www.guildwars2.com/) helper tools: a Windows desktop companion app,
backed by reusable, strongly-typed .NET libraries for the GW2 API that are published as NuGet
packages in their own right.

## What's inside

| Component | What it is | Distribution |
| --- | --- | --- |
| **Gw2Gizmos** (desktop app) | Windows tray companion that ingests GW2 data and surfaces notifications, logs, and a dashboard | [GitHub Releases](https://github.com/gosferano/gw2-gizmos/releases) installer |
| **Gw2Gizmos.Gw2Api.Client** | Typed HTTP client for the GW2 API v2 (resilience, `IHttpClientFactory`, bulk/pagination) | [NuGet](https://www.nuget.org/packages/Gw2Gizmos.Gw2Api.Client) |
| **Gw2Gizmos.Gw2Api.Contract** | Request/response DTOs for the GW2 API v2 (a dependency of the client) | [NuGet](https://www.nuget.org/packages/Gw2Gizmos.Gw2Api.Contract) |

Each component is **versioned and released independently** — see [Releases & versioning](#releases--versioning).

> **Status:** pre-release. Components are baselined at `0.0.0` until the first tagged release, so the
> NuGet packages and installer above are not published yet.

---

## Desktop app

**Gw2Gizmos** is a Windows companion that runs a background worker to ingest GW2 API data into a local
SQLite database, then presents it through a [WPF-UI](https://github.com/lepoco/wpfui) shell:

- **Dashboard** — at-a-glance status of the data ingestion
- **Notifications** — in-app feed plus native Windows toast notifications
- **Logs** — live viewer tailing the worker's output
- **Settings** — API key and preferences (the API key is stored encrypted via Windows DPAPI)

It self-updates via [Velopack](https://github.com/velopack/velopack): install once, and new releases are
downloaded and applied on the next restart.

**Install:** download the latest `Setup.exe` from [Releases](https://github.com/gosferano/gw2-gizmos/releases)
and run it. The app installs per-user under `%LocalAppData%\Gw2Gizmos`, with its database and encrypted
key in `%APPDATA%\Gw2Gizmos`.

## GW2 API libraries

For .NET developers who just want to call the GW2 API. The client depends on the contract package and
pulls it in automatically — you normally only reference the client.

```bash
dotnet add package Gw2Gizmos.Gw2Api.Client
```

```csharp
using Gw2Gizmos.Gw2Api.Client;

// apiKey is optional — omit it for public endpoints.
var client = Gw2ApiClient.Create(apiKey);

Account? account = await client.V2.Account.GetBlob();
var items = await client.V2.Items.GetByIds([19684, 19721]);
```

Construction is routed through `IHttpClientFactory` (pooled handlers, fresh DNS), and calls retry
transient failures and HTTP 429 — honoring `Retry-After` — with a per-attempt timeout. For dependency
injection use `services.AddGw2ApiClient()`. Full quickstart, the DI path, and the endpoint coverage
checklist are in the [client README](src/Gw2Gizmos.Gw2Api.Client/README.md).

Reference **Gw2Gizmos.Gw2Api.Contract** directly only if you want the DTOs without the client.

## Repository layout

```
Gw2Gizmos.sln
Directory.Build.props        # universal build settings (net10, nullable-as-error)
Directory.Packages.props     # central package management for the whole repo
src/
  Directory.Build.props      # + packaging metadata & SourceLink (shippable libs)
  Gw2Gizmos.Gw2Api.Contract/ # ── NuGet package
  Gw2Gizmos.Gw2Api.Client/   # ── NuGet package
  Gw2Gizmos.Desktop/         # ── desktop app (builds Gw2Gizmos.exe)
  Gw2Gizmos.Data.Worker[.Cli]/   # background data-ingestion worker
  Gw2Gizmos.Data.EntityFramework/ # EF Core model + SQLite store
  …and supporting CLIs/utilities
tools/
  Gw2Gizmos.Gw2Api.NullabilityAudit/  # samples the live API to audit DTO nullability
```

All projects target **.NET 10** and use Central Package Management; shared settings live in the
root `Directory.*.props`.

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build Gw2Gizmos.sln                       # build everything
dotnet run --project src/Gw2Gizmos.Desktop       # run the desktop app (Windows)
dotnet test                                       # (tests planned under tests/)
```

The desktop app is Windows-only (WPF + DPAPI + Velopack); the API libraries are cross-platform.

## Releases & versioning

Each component carries its own `<Version>` and is released by GitHub Actions on push to `main` when
that version is new (bumping a project's version is what triggers its release):

| Component | Tag | Published to |
| --- | --- | --- |
| Contract | `gw2-api-contract-<v>` | NuGet.org |
| Client | `gw2-api-client-<v>` | NuGet.org |
| Desktop | `gw2gizmos-desktop-<v>` | GitHub Release (Velopack installer) |

NuGet publishing uses [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
(OIDC, no API keys). Packages ship with SourceLink and `.snupkg` symbols for step-into debugging.

## License

[MIT](LICENSE) © gosferano
