# Gw2Gizmos.Gw2Api.NullabilityAudit

A maintenance utility that finds which `Gw2Gizmos.Gw2Api.Contract` DTO fields are *genuinely
optional* — by empirically sampling the live GW2 v2 API — and (optionally) makes those fields
nullable in the Contract source.

It exists because GW2's API is only loosely documented for nullability: a field's absence or
`null` in real responses is the source of truth, not the wiki. Rather than auditing ~270 DTOs by
hand, this tool samples thousands of real objects per endpoint and reports what's actually optional.

## Two modes

### 1. Sample (default)

```bash
dotnet run --project tools/Gw2Gizmos.Gw2Api.NullabilityAudit
```

Lists each endpoint's ids, bulk-fetches a strided sample (up to 4000 objects/endpoint), and records
per field-path how often it is **present / null / absent**. Writes `nullability-report.json` to the
build output directory (`bin/Debug/net10.0/`).

- No API key needed for the default 48 game-data endpoints.
- Set `GW2_API_KEY` (env) to also reach token-gated endpoints (these are **not** in the default
  list yet — pass them explicitly: `dotnet run -- account/bank characters …`).
- Pass endpoint names as args to sample a custom subset: `dotnet run -- items skills traits`.

### 2. Apply

```bash
dotnet run --project tools/Gw2Gizmos.Gw2Api.NullabilityAudit -- apply           # dry-run (prints the diff)
dotnet run --project tools/Gw2Gizmos.Gw2Api.NullabilityAudit -- apply --write    # edits the .cs files
```

Reads the report, maps each flagged field-path back to the exact Contract property via reflection,
and makes the **non-collection** ones nullable (`string … = null!` → `string?`, `int` → `int?`),
preserving each file's BOM + line endings. Dry-run by default; `--write` saves.

After `--write`, build the solution — `WarningsAsErrors=Nullable` will surface any consumer that now
dereferences a newly-nullable field without a guard (that's the audit working: those are real
unchecked nulls).

## How it handles polymorphism

Many GW2 hierarchies are polymorphic (`Item`/`Skin` subtypes, `SkillFact`/`TraitFact` in `facts[]`,
etc.). A naive flat JSON-path walk conflates siblings — e.g. `details` looks "absent" across the
items endpoint only because `ItemSimple` has no details at all, not because any `Armor` ever lacks
it.

To avoid that, the sampler qualifies every object's path by its `type` discriminator
(`facts[Buff].duration` vs `facts[Damage].*`, `[Armor].details` vs `[Weapon].details`), and the
apply step uses the `[Disc]` marker to descend into the matching C# subtype (ignoring it when it's a
value-level discriminator like an armor slot). This keeps each subtype's tally isolated.

## Reading the report / judgment calls

Each optional field carries `everNull` / `everAbsent` plus `nullCount` / `absentCount` / `seen` /
`ofParents`. Strong signals (a field absent in a large fraction of its own subtype, e.g. buff
`duration` missing on ~18% of heal-skill buff facts) are safe nullable changes. Thin signals
(`absent=1` of hundreds) may be a single malformed record rather than a true optional — cross-check
against the [GW2 wiki](https://wiki.guildwars2.com/wiki/API:2) or a reference client like
[GW2Sharp](https://github.com/Archomeda/Gw2Sharp) before keeping them.

Collections are never made nullable — they're initialized empty in the Contract.

## Notes

- Outside `src/`, so it never ships; inherits the repo-wide `Directory.Build.props` (net10, nullable)
  but not the packaging metadata.
- Re-run the sample whenever GW2 adds fields or you want to refresh the evidence.
