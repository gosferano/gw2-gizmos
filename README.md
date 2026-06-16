# Gw2Gizmos

A suite of [Guild Wars 2](https://www.guildwars2.com/) helper tools: a Windows desktop companion app,
backed by reusable, strongly-typed .NET libraries for the GW2 API that are published as NuGet packages in
their own right.

## Components

| Component | What it is | Distribution | Docs |
| --- | --- | --- | --- |
| **Gw2Gizmos** (desktop app) | Windows tray companion that ingests GW2 data and surfaces a dashboard, account/character/session views, events, prices, and notifications | [GitHub Releases](https://github.com/gosferano/gw2-gizmos/releases) installer | [README](src/Gw2Gizmos.Desktop/README.md) |
| **Gw2Gizmos.Gw2Api.Client** | Typed HTTP client for the GW2 API v2 (resilience, `IHttpClientFactory`, bulk/pagination) | [NuGet](https://www.nuget.org/packages/Gw2Gizmos.Gw2Api.Client) | [README](src/Gw2Gizmos.Gw2Api.Client/README.md) |
| **Gw2Gizmos.Gw2Api.Contract** | Request/response DTOs for the GW2 API v2 (a dependency of the client) | [NuGet](https://www.nuget.org/packages/Gw2Gizmos.Gw2Api.Contract) | [README](src/Gw2Gizmos.Gw2Api.Contract/README.md) |
| **Gw2Gizmos.Data.Worker** | Background data-ingestion engine (the desktop's worker process) | — (internal) | [README](src/Gw2Gizmos.Data.Worker/README.md) |

Each component is **versioned and released independently**.

## Desktop app

A Windows companion that runs a background worker to ingest GW2 API data into a local SQLite database,
then presents it through a [WPF-UI](https://github.com/lepoco/wpfui) shell:

- **Dashboard** — at-a-glance app/worker/account/session stats.
- **Account** — wallet, material storage, bank, shared inventory, and per-character inventories, with
  account-wide item totals reconstructed from an event-sourced history.
- **Sessions** — play sessions detected from MumbleLink, with per-character switches and the currency/items
  "hoarded" during each.
- **Events** — scheduled world-boss / meta-event reminders with native Windows toasts.
- **Items** — the item catalog with live trading-post prices and crafting-cost.
- **Notifications & Logs** — an in-app feed (plus toasts) and a live viewer tailing the worker.
- **Settings** — feature toggles, sync intervals, launch-at-startup, and per-account stored-data management.

It self-updates via [Velopack](https://github.com/velopack/velopack): install once, and new releases are
downloaded and applied on the next restart (the dashboard shows a "restart to update" prompt when one's
ready). API keys are stored encrypted via Windows DPAPI.

**Install:** download the latest `Setup.exe` from [Releases](https://github.com/gosferano/gw2-gizmos/releases)
and run it. The app installs per-user under `%LocalAppData%\Gw2Gizmos`, with its database and encrypted key
in `%APPDATA%\Gw2Gizmos`. See the [desktop README](src/Gw2Gizmos.Desktop/README.md) for how it's built.

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

Construction is routed through `IHttpClientFactory` (pooled handlers, fresh DNS), and calls retry transient
failures and HTTP 429 — honoring `Retry-After` — with a per-attempt timeout. For dependency injection use
`services.AddGw2ApiClient()`. Full quickstart, the DI path, and endpoint coverage are in the
[client README](src/Gw2Gizmos.Gw2Api.Client/README.md). Reference **Gw2Gizmos.Gw2Api.Contract** directly
only if you want the DTOs without the client.

## Contributing

Build instructions, repository layout, the architecture overview, migrations, and the release process are
in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © gosferano
