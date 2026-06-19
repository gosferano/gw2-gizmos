# Gw2Gizmos.Wiki.DataScraper

A maintenance utility that scrapes game data the official GW2 API doesn't expose from the
[GW2 wiki](https://wiki.guildwars2.com), and writes it to a JSON artifact.

Today it scrapes **Mystic Forge recipes** — these are not in the `/v2/recipes` API, but they're needed to
value untradeable currencies and items by the tradeable outputs they can be forged into (part of an honest
account-worth calculation).

## Usage

```bash
dotnet run --project tools/Gw2Gizmos.Wiki.DataScraper
```

Writes `mystic-forge-recipes.json` to the build output directory (`bin/Debug/net10.0/`):

```jsonc
{
  "source": "https://wiki.guildwars2.com",
  "category": "Category:Mystic Forge recipes",
  "count": 2159,
  "recipes": [
    {
      "Output": "Mystic Clover",
      "OutputCount": 1,
      "Ingredients": [
        { "Count": 1, "Name": "Obsidian Shard" },
        { "Count": 1, "Name": "Mystic Coin" },
        { "Count": 1, "Name": "Glob of Ectoplasm" },
        { "Count": 6, "Name": "Philosopher's Stone" }
      ],
      "SourcePage": "Mystic Clover"
    }
  ]
}
```

## How it works

The wiki's Semantic MediaWiki layer has no clean recipe schema (recipe subobjects aren't queryable by
property), so the tool parses recipe templates out of page wikitext:

1. Enumerates every page in `Category:Mystic Forge recipes` via the MediaWiki API.
2. Fetches their current wikitext in bulk via **`Special:Export`** (many pages per POST — far fewer requests
   than the API's per-page content endpoint, which keeps us under the wiki CDN's burst protection).
3. Brace-matches every `{{recipe | ... }}` template and keeps the ones whose `source` is the Mystic Forge
   (written as either `Mystic Forge` or the shorthand `mystic`, used on infused ascended trinkets).
4. Extracts the output item (from the recipe's `link`/`result`/`name`, falling back to the page title when a
   dedicated item page omits it) and the ingredients (`ingredient1..N`, each `"<qty> <name>"`), folding
   same-named ingredient slots into one summed quantity.

## Being a good wiki citizen

- Descriptive, contactable `User-Agent`; `maxlag=5` so the wiki sheds our load first if its replicas lag.
- Requests are paced (~0.6 s apart) and retried with exponential backoff on 403/429/5xx; bulk export keeps
  the total request count low (~20 requests for ~1,900 pages).
- Recipe ingredients and quantities are *facts* (not copyrightable); the artifact is cached so we re-fetch
  rarely. Re-run only when the Mystic Forge gets new recipes.

## Known limitations

- **`{{recipe table}}` pages are skipped.** A handful of pages (≈60) list recipes via a dynamic SMW query
  template (e.g. "forge any 4 of a weapon type → a random skin") rather than concrete `{{recipe}}` blocks.
  These are random-skin forges with no fixed gold value, so they're irrelevant to valuation and not worth
  resolving the live query for. The run prints how many pages this affects.

## Notes

- Outside `src/`, so it never ships; inherits the repo-wide `Directory.Build.props` (net10, nullable) but not
  the packaging metadata under `src/`.
- The next step (separate work) is curating the artifact into `Gw2Gizmos.Data.Static` and teaching the
  valuation model to cost untradeable items through these recipes.
