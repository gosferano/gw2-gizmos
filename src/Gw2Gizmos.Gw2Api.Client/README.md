# Gw2Gizmos.Gw2Api.Client

A typed .NET client for the [Guild Wars 2 API v2](https://wiki.guildwars2.com/wiki/API:2): strongly-typed
endpoints, bulk/pagination helpers, built-in resilience (retry + rate-limit/429 handling), and a managed
`HttpClient` via `IHttpClientFactory`. The request/response types come from the
`Gw2Gizmos.Gw2Api.Contract` package, pulled in automatically.

## Install

```
dotnet add package Gw2Gizmos.Gw2Api.Client
```

## Quick start (no DI)

```csharp
using Gw2Gizmos.Gw2Api.Client;

// apiKey is optional — omit it for public endpoints.
var client = Gw2ApiClient.Create(apiKey);

Account? account = await client.V2.Account.GetBlob();
var characters = await client.V2.Characters.GetByIds(["My Character"]);
```

## With dependency injection

```csharp
services.AddGw2ApiClient(); // registers IGw2ApiClientFactory with retry/429 resilience
// inject IGw2ApiClientFactory, then: clients.Create(apiKey)
```

## HttpClient & resilience

Construction is routed through `IHttpClientFactory`, so handlers are pooled and rotated (no socket
exhaustion, fresh DNS). Calls retry transient failures and HTTP 429 (honoring `Retry-After`) with a
per-attempt timeout. For a custom handler/proxy: `AddGw2ApiClient(builder => /* configure */)`.

- **`Gw2ApiClient.Create(apiKey)`** — simplest; reuses a process-wide default factory (build once). Ideal
  for scripts/CLIs.
- **`AddGw2ApiClient()` + `IGw2ApiClientFactory`** — for apps using dependency injection.

## Supported endpoints

- [x] /v2/account
- [x] /v2/account/achievements
- [x] /v2/account/bank
- [x] /v2/account/buildstorage
- [x] /v2/account/dailycrafting
- [x] /v2/account/dungeons
- [x] /v2/account/dyes
- [x] /v2/account/emotes
- [x] /v2/account/finishers
- [x] /v2/account/gliders
- [x] /v2/account/home
- [x] /v2/account/home/cats
- [x] /v2/account/home/nodes
- [x] /v2/account/inventory
- [x] /v2/account/jadebots
- [x] /v2/account/legendaryarmory
- [x] /v2/account/luck
- [ ] /v2/account/mail - API not active
- [x] /v2/account/mailcarriers
- [x] /v2/account/mapchests
- [x] /v2/account/masteries
- [ ] /v2/account/mastery/points
- [x] /v2/account/materials
- [x] /v2/account/minis
- [x] /v2/account/mounts
- [x] /v2/account/mounts/skins
- [x] /v2/account/mounts/types
- [x] /v2/account/novelties
- [x] /v2/account/outfits
- [x] /v2/account/progression
- [x] /v2/account/pvp/heroes
- [x] /v2/account/raids
- [x] /v2/account/recipes
- [x] /v2/account/skiffs
- [x] /v2/account/skins
- [x] /v2/account/titles
- [x] /v2/account/wallet
- [x] /v2/account/wizardsvault/daily
- [x] /v2/account/wizardsvault/listings
- [x] /v2/account/wizardsvault/special
- [x] /v2/account/wizardsvault/weekly
- [x] /v2/account/worldbosses
- [x] /v2/achievements
- [x] /v2/achievements/categories
- [ ] /v2/achievements/daily - API not active
- [ ] /v2/achievements/daily/tomorrow - API not active
- [x] /v2/achievements/groups
- [ ] /v2/adventures - API not active
- [ ] /v2/adventures/:id/leaderboards - API not active
- [ ] /v2/adventures/:id/leaderboards/:board/:region - API not active
- [x] /v2/backstory/answers
- [x] /v2/backstory/questions
- [x] /v2/build
- [x] /v2/characters
- [x] /v2/characters/:id/backstory
- [x] /v2/characters/:id/buildtabs
- [x] /v2/characters/:id/buildtabs/active
- [x] /v2/characters/:id/core
- [x] /v2/characters/:id/crafting
- [ ] /v2/characters/:id/dungeons - API not active
- [x] /v2/characters/:id/equipment
- [x] /v2/characters/:id/equipmenttabs
- [x] /v2/characters/:id/equipmenttabs/active
- [x] /v2/characters/:id/heropoints
- [x] /v2/characters/:id/inventory
- [x] /v2/characters/:id/quests
- [x] /v2/characters/:id/recipes
- [x] /v2/characters/:id/sab
- [ ] /v2/characters/:id/skills - use /v2/characters/:id/buildtabs instead
- [ ] /v2/characters/:id/specializations - use /v2/characters/:id/buildtabs instead
- [x] /v2/characters/:id/training
- [x] /v2/colors
- [x] /v2/commerce/delivery
- [x] /v2/commerce/exchange
- [x] /v2/commerce/listings
- [x] /v2/commerce/prices
- [x] /v2/commerce/transactions
- [x] /v2/continents
- [ ] /v2/createsubtoken
- [x] /v2/currencies
- [x] /v2/dailycrafting
- [x] /v2/dungeons
- [x] /v2/emblem
- [x] /v2/emotes
- [ ] /v2/events - API not active
- [ ] /v2/events-state - API not active
- [x] /v2/files
- [x] /v2/finishers
- [ ] /v2/gemstore/catalog - API not active
- [x] /v2/gliders
- [x] /v2/guild/:id
- [ ] /v2/guild/:id/log
- [x] /v2/guild/:id/members
- [x] /v2/guild/:id/ranks
- [x] /v2/guild/:id/stash
- [x] /v2/guild/:id/storage
- [x] /v2/guild/:id/teams - untested
- [x] /v2/guild/:id/treasury
- [x] /v2/guild/:id/upgrades
- [x] /v2/guild/permissions
- [x] /v2/guild/search
- [x] /v2/guild/upgrades
- [x] /v2/home
- [x] /v2/home/cats
- [x] /v2/home/nodes
- [x] /v2/items
- [x] /v2/itemstats
- [x] /v2/jadebots
- [x] /v2/legendaryarmory
- [x] /v2/legends
- [x] /v2/mailcarriers
- [x] /v2/mapchests
- [x] /v2/maps
- [x] /v2/masteries
- [x] /v2/materials
- [x] /v2/minis
- [x] /v2/mounts
- [x] /v2/mounts/skins
- [x] /v2/mounts/types
- [x] /v2/novelties
- [x] /v2/outfits
- [x] /v2/pets
- [x] /v2/professions
- [x] /v2/pvp
- [x] /v2/pvp/amulets
- [x] /v2/pvp/games
- [x] /v2/pvp/heroes
- [x] /v2/pvp/ranks
- [ ] /v2/pvp/rewardtracks - API not active
- [ ] /v2/pvp/runes - API not active
- [x] /v2/pvp/seasons
- [x] /v2/pvp/seasons/:id/leaderboards
- [x] /v2/pvp/seasons/:id/leaderboards/:board/:region
- [ ] /v2/pvp/sigils - API not active
- [x] /v2/pvp/standings
- [x] /v2/pvp/stats
- [ ] /v2/quaggans
- [x] /v2/quests
- [x] /v2/races
- [x] /v2/raids
- [x] /v2/recipes
- [x] /v2/recipes/search
- [x] /v2/skiffs
- [x] /v2/skills
- [x] /v2/skins
- [x] /v2/specializations
- [x] /v2/stories
- [x] /v2/stories/seasons
- [x] /v2/titles
- [x] /v2/tokeninfo
- [x] /v2/traits
- [ ] /v2/vendors - API not active
- [x] /v2/wizardsvault
- [x] /v2/wizardsvault/listings
- [x] /v2/wizardsvault/objectives
- [x] /v2/worldbosses
- [x] /v2/worlds
- [x] /v2/wvw/abilities
- [x] /v2/wvw/matches
- [x] /v2/wvw/matches/overview
- [x] /v2/wvw/matches/scores
- [x] /v2/wvw/matches/stats
- [ ] /v2/wvw/matches/stats/:id/guilds/:guild_id
- [ ] /v2/wvw/matches/stats/:id/teams/:team/top/kdr
- [ ] /v2/wvw/matches/stats/:id/teams/:team/top/kills
- [x] /v2/wvw/objectives
- [x] /v2/wvw/ranks
- [ ] /v2/wvw/rewardtracks - API not active
- [x] /v2/wvw/upgrades
