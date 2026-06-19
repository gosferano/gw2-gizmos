# Gw2Gizmos.Wiki.DataScraper

A maintenance utility that scrapes game data the official GW2 API doesn't expose from the
[GW2 wiki](https://wiki.guildwars2.com), and writes it to JSON artifacts.

It exists to support an **honest account-worth calculation**: valuing untradeable currencies/items by the
tradeable things they convert into. Neither of the two datasets it scrapes is in the GW2 v2 API:

- **Mystic Forge recipes** — what an untradeable input (e.g. a Spirit Shard via Philosopher's Stones) can be
  forged into.
- **Vendor prices** of the ingredients those recipes use — so a recipe's cost (and a vendor-only ingredient's
  value) can be computed.

## Usage

Two modes. The vendor pass is **separate on purpose** — the recipe pass fires enough requests to prime the
wiki CDN's burst protection, so running vendors "cold" (and slowly) avoids 403s.

```bash
# 1. Mystic Forge recipes  ->  mystic-forge-recipes.json
dotnet run --project tools/Gw2Gizmos.Wiki.DataScraper

# 2. Vendor prices for those recipes' ingredients  ->  vendor-items.json   (run after step 1)
dotnet run --project tools/Gw2Gizmos.Wiki.DataScraper -- vendors
```

Both artifacts land in the build output directory (`bin/Debug/net10.0/`). As of 2026-06: **2,159 recipes**
and **488 vendor-sold ingredients**.

### `mystic-forge-recipes.json`

```jsonc
{
  "Output": "Mystic Clover", "OutputId": 19675, "OutputCount": 1,
  "Ingredients": [
    { "Count": 1, "Name": "Obsidian Shard", "Id": 19925 },
    { "Count": 6, "Name": "Philosopher's Stone", "Id": 20796 }
  ],
  "SourcePage": "Mystic Clover"
}
```

Output/ingredient ids come from a resolve pass (SMW `Has game id`); a few stay null when the name is a generic
non-item page (e.g. "Ascended armor").

### `vendor-items.json`

```jsonc
{
  "GameId": 19663, "Item": "Bottle of Elonian Wine",
  "Offers": [
    { "Vendor": "Miyani", "Quantity": 1,
      "Cost": [ { "Value": 2504, "Currency": "Coin", "ItemId": null, "CurrencyId": 1 } ] }
  ]
}
```

Costs can have multiple components (e.g. `100 Volatile Magic + 96 Coin`). Each component carries the cost's id,
split by kind: **`ItemId`** when the currency is a tradeable item (`Bauble Bubble` → 41886), **`CurrencyId`**
when it's an account currency (`Coin` → 1, `Volatile Magic` → 45). ~1% of components resolve to neither —
alias spellings the wiki uses in cost fields ("Gold", "Laurels", "Globs of Ectoplasm") that don't match a
canonical page/currency name; reconcile those by name at curation time.

## How it works

The wiki's Semantic MediaWiki layer has no queryable recipe schema, so **recipes** are parsed from page
wikitext:

1. Enumerate `Category:Mystic Forge recipes`.
2. Fetch current wikitext in bulk via **`Special:Export`** (many pages per POST — far fewer requests than the
   API's per-page content endpoint, which keeps us under the CDN's burst protection).
3. Brace-match every `{{recipe | ... }}` and keep those whose `source` is the Mystic Forge (written as
   `Mystic Forge` or the shorthand `mystic`, used on infused trinkets). Output comes from `link`/`result`/
   `name`, falling back to the page title; same-named ingredient slots are folded into one summed quantity.
   Finally, resolve every output/ingredient name → game id (SMW `Has game id`, OR-batched ≤12).

**Vendor prices** are structured in SMW (`[[Sells item::X]]` / `Has vendor` / `Has item cost` / …), queried via
the `ask` API. We pull the **whole catalog**, keyed by GW2 item id (`?Sells item.Has game id`), keeping every
item — a vendor can undercut the trading post, so valuation needs `min(vendor, TP)` for everything, not just
untradeable items.

4. SMW silently truncates any single query at a result-offset cap (~6,000), so we **partition** instead of
   sweeping. First a quick sweep discovers the distinct cost-currencies, then each currency is paged to
   completion (`[[Has vendor::+]][[Has item cost.Has item currency::C]]`).
5. A currency still over the cap (Coin, Karma) is **sub-split by the sold item's rarity, then its type** (split
   values discovered from the over-cap fetch itself, no hardcoded vocabulary). A leaf that *still* exceeds the
   cap (the huge karma recipe-sheet catalog) is finished with **keyset pagination**: sort by the item's game id
   and page `[[Sells item.Has game id::>lastId]]`, so the growing (capped) offset is never used. Multi-currency
   offers repeat across partitions and are de-duped by content. Paced ~0.5 s, retried with backoff.
6. Cost currencies are then resolved to ids two ways so a price is joinable: the wiki `Has game id` pass gives
   `ItemId` for item-currencies, and one `/v2/currencies` call gives `CurrencyId` for account currencies.

As of 2026-06 this yields **~10,850 vendor-sold items, no partition left capped**.

## Being a good wiki citizen

Descriptive contactable `User-Agent`, `maxlag=5`, pacing, and exponential backoff on 403/429/5xx. Recipe
ingredients and vendor prices are *facts* (not copyrightable); the artifacts are cached — re-run only when the
game changes.

## Known limitations

- **`{{recipe table}}` pages are skipped** (~60): dynamic SMW skin-forge queries (random weapon/armor skins,
  no fixed value) rather than concrete recipes. The run prints how many pages this affects.
- **A few ids stay null:** recipe outputs whose name is a generic non-item page ("Ascended armor"), and ~1% of
  cost components whose currency is an alias spelling the wiki uses in cost fields ("Gold", "Laurels", "Globs of
  Ectoplasm") that matches neither a canonical item page nor a `/v2/currencies` name. Reconcile those by name at
  curation time against the app's own item/currency tables.

## Notes

- Outside `src/`, so it never ships; inherits the repo-wide `Directory.Build.props` (net10, nullable).
- Next (separate work): curate these artifacts into `Gw2Gizmos.Data.Static` and teach the valuation model to
  cost untradeable items through the recipes + vendor prices.
