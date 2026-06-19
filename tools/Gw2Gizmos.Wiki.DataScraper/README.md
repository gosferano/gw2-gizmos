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
  "Output": "Mystic Clover", "OutputCount": 1,
  "Ingredients": [
    { "Count": 1, "Name": "Obsidian Shard" },
    { "Count": 6, "Name": "Philosopher's Stone" }
  ],
  "SourcePage": "Mystic Clover"
}
```

### `vendor-items.json`

```jsonc
{
  "Item": "Mystic Crystal",
  "Offers": [
    { "Vendor": "Miyani", "Quantity": 5, "Cost": [ { "Value": 3, "Currency": "Spirit Shard" } ] }
  ]
}
```

Costs can have multiple components (e.g. Obsidian Shard = `100 Volatile Magic + 96 Coin`); a coin price uses
`Currency: "Coin"`.

## How it works

The wiki's Semantic MediaWiki layer has no queryable recipe schema, so **recipes** are parsed from page
wikitext:

1. Enumerate `Category:Mystic Forge recipes`.
2. Fetch current wikitext in bulk via **`Special:Export`** (many pages per POST — far fewer requests than the
   API's per-page content endpoint, which keeps us under the CDN's burst protection).
3. Brace-match every `{{recipe | ... }}` and keep those whose `source` is the Mystic Forge (written as
   `Mystic Forge` or the shorthand `mystic`, used on infused trinkets). Output comes from `link`/`result`/
   `name`, falling back to the page title; same-named ingredient slots are folded into one summed quantity.

**Vendor prices** are structured in SMW (`[[Sells item::X]]` / `Has vendor` / `Has item cost` / …), queried via
the `ask` API. We pull the **whole catalog**, keyed by GW2 item id (`?Sells item.Has game id`), keeping every
item — a vendor can undercut the trading post, so valuation needs `min(vendor, TP)` for everything, not just
untradeable items.

4. SMW silently truncates any single query at a result-offset cap (~6,000), so we **partition** instead of
   sweeping. First a quick sweep discovers the distinct cost-currencies, then each currency is paged to
   completion (`[[Has vendor::+]][[Has item cost.Has item currency::C]]`).
5. A currency still over the cap (Coin, Karma) is **sub-split by the sold item's rarity**. Multi-currency
   offers appear in several partitions, so offers are de-duped by content. Paced ~1 s, retried with backoff;
   a partition that *still* exceeds the cap after the rarity split is logged as `*** STILL CAPPED ***` so
   incompleteness is never silent.

As of 2026-06 this yields **~10,600 vendor-sold items**.

## Being a good wiki citizen

Descriptive contactable `User-Agent`, `maxlag=5`, pacing, and exponential backoff on 403/429/5xx. Recipe
ingredients and vendor prices are *facts* (not copyrightable); the artifacts are cached — re-run only when the
game changes.

## Known limitations

- **`{{recipe table}}` pages are skipped** (~60): dynamic SMW skin-forge queries (random weapon/armor skins,
  no fixed value) rather than concrete recipes. The run prints how many pages this affects.
- **Two vendor buckets stay capped:** `Coin/Basic` and `Karma/Rare` exceed the offset cap even after the rarity
  split (they're huge sets of low-value vendor *gear* — basic coin weapons, rare karma armor stat-combos). These
  aren't recipe ingredients and their worth isn't a vendor buy-price, so they're left partial (and logged). To
  close them, add a second sub-split dimension (item type) in `FetchPartitionAsync`. Valuation-relevant items
  (crafting mats, forge/currency conversions) are fully covered.

## Notes

- Outside `src/`, so it never ships; inherits the repo-wide `Directory.Build.props` (net10, nullable).
- Next (separate work): curate these artifacts into `Gw2Gizmos.Data.Static` and teach the valuation model to
  cost untradeable items through the recipes + vendor prices.
