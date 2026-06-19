using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Scrapes game data the GW2 API doesn't expose from the wiki. Two modes (the vendor pass is separate so it
// runs "cold" — the recipe pass fires enough requests to prime the wiki CDN's burst protection):
//
//   dotnet run --project tools/Gw2Gizmos.Wiki.DataScraper            # Mystic Forge recipes → mystic-forge-recipes.json
//   dotnet run --project tools/Gw2Gizmos.Wiki.DataScraper -- vendors # vendor prices for those recipes' ingredients → vendor-items.json
//
// Why the wiki: Mystic Forge recipes and vendor buy-prices aren't in the official v2 API, but they're needed
// to value untradeable currencies/items (the forge outputs they make, the vendor cost of recipe ingredients)
// for an honest account-worth calculation. Recipes are parsed from {{recipe|source=Mystic Forge}} wikitext;
// vendor offers come from the structured Semantic MediaWiki [[Sells item::X]] relation.

const string ApiUrl = "https://wiki.guildwars2.com/api.php";
const string ExportUrl = "https://wiki.guildwars2.com/index.php?title=Special:Export";
const string Category = "Category:Mystic Forge recipes";
const string UserAgent =
    "Gw2Gizmos-WikiDataScraper/0.1 (personal account-valuation tool; +https://github.com/gosferano/gw2-gizmos)";

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

// Vendor mode: read the ingredients of the already-scraped recipes and look up who sells each, for how much.
if (args.Length > 0 && args[0].Equals("vendors", StringComparison.OrdinalIgnoreCase))
{
    await ScrapeVendorsAsync();
    return;
}

// 1. Enumerate every page in the Mystic Forge recipes category.
List<string> titles = await ListCategoryPagesAsync(Category);
Console.Error.WriteLine($"{titles.Count} page(s) in {Category}.");

// 2. Fetch wikitext in bulk via Special:Export (many pages per request → far fewer hits than the API's
//    per-page content endpoint, which keeps us under the CDN's burst protection) and parse the recipes.
var recipes = new List<Recipe>();
int noTemplatePages = 0;   // page has no {{recipe}} of its own (e.g. it transcludes a /Recipes subpage)
int nonMysticPages = 0;    // page has {{recipe}} blocks but none with source = Mystic Forge
var recipeOpener = new Regex(@"\{\{\s*recipe\s*[|}]", RegexOptions.IgnoreCase);
var nonMysticSamples = new List<string>();
const int Batch = 100;
for (int i = 0; i < titles.Count; i += Batch)
{
    List<string> slice = titles.GetRange(i, Math.Min(Batch, titles.Count - i));
    foreach ((string title, string wikitext) in await FetchWikitextAsync(slice))
    {
        List<Recipe> pageRecipes = ParseMysticForgeRecipes(title, wikitext).ToList();
        recipes.AddRange(pageRecipes);
        if (pageRecipes.Count == 0)
        {
            if (recipeOpener.IsMatch(wikitext))
            {
                nonMysticPages++;
                if (nonMysticSamples.Count < 25)
                {
                    IEnumerable<string> srcs = ExtractTemplates(wikitext, "recipe")
                        .Select(b => ParseTemplateFields(b).GetValueOrDefault("source") ?? "(none)");
                    nonMysticSamples.Add($"{title}: [{string.Join(" | ", srcs)}]");
                }
            }
            else
            {
                noTemplatePages++;
            }
        }
    }

    Console.Error.WriteLine(
        $"  {Math.Min(i + Batch, titles.Count)}/{titles.Count} pages parsed, {recipes.Count} recipe(s) so far…");
    await Task.Delay(600); // be a courteous wiki citizen between requests
}

Console.Error.WriteLine(
    $"Pages with no recipe template of their own (likely transclusions): {noTemplatePages}; "
    + $"pages with recipes but none Mystic Forge: {nonMysticPages}.");
Console.Error.WriteLine("Sample of rejected pages (title: [recipe source values]):");
foreach (string s in nonMysticSamples)
{
    Console.Error.WriteLine($"  {s}");
}

// 3. Write the artifact next to the binary.
string outPath = Path.Combine(AppContext.BaseDirectory, "mystic-forge-recipes.json");
string json = JsonSerializer.Serialize(
    new
    {
        source = "https://wiki.guildwars2.com",
        category = Category,
        count = recipes.Count,
        recipes = recipes.OrderBy(r => r.Output, StringComparer.OrdinalIgnoreCase).ToList(),
    },
    new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(outPath, json);
Console.Error.WriteLine($"Wrote {recipes.Count} recipe(s) to {outPath}");
Console.Error.WriteLine("Next: 'dotnet run -- vendors' to scrape vendor prices for these recipes' ingredients.");

// --- helpers ---

// Vendor mode entry point: read the recipe artifact's ingredients and write their vendor offers.
// Pull the entire vendor catalog from SMW, keyed by GW2 item id (what Data.Static needs). We keep ALL items,
// not just untradeable ones — a vendor can undercut the trading post, so valuation needs min(vendor, TP) for
// everything. [[Has vendor::+]] in one sweep hits SMW's result-offset cap (~6000) and silently truncates, so
// instead we partition by cost-currency: each partition is small enough to page to completion. The chained
// ?Sells item.Has game id printout supplies each item's id.
async Task ScrapeVendorsAsync()
{
    // Phase 1: discover the distinct cost-currencies. Currencies are far fewer than offers, so this sweep —
    // even though it itself truncates at the cap — sees every currency type in practice.
    var currencies = new SortedSet<string>(StringComparer.Ordinal);
    Console.Error.WriteLine("Discovering cost-currencies…");
    await ForEachOfferAsync("[[Has vendor::+]]|?Has item cost", po =>
    {
        foreach (CostComponent c in ReadCost(po))
        {
            currencies.Add(c.Currency);
        }
    });
    Console.Error.WriteLine($"Found {currencies.Count} cost-currenc(ies). Fetching each partition…");

    // Phase 2: fetch each currency's offers to completion. A multi-currency offer (e.g. coin + a token) shows
    // up in every one of its currencies' partitions, so de-dup offers by a content key. A currency that still
    // exceeds the cap (Coin) is sub-split by the sold item's rarity until every leaf fits.
    var byItem = new Dictionary<string, VendorItem>(StringComparer.OrdinalIgnoreCase);
    var seenOffers = new HashSet<string>();
    try
    {
        foreach (string currency in currencies)
        {
            // SMW query values can't contain the markup delimiters.
            if (currency.IndexOfAny(['|', '[', ']', '{', '}', '=', '#', '<', '>']) >= 0)
            {
                continue;
            }

            await FetchPartitionAsync(
                currency,
                $"[[Has vendor::+]][[Has item cost.Has item currency::{currency}]]",
                byItem,
                seenOffers,
                canSplit: true);
        }
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine(
            $"  [warn] wiki throttled us ({ex.Message}); stopping early with {byItem.Count} item(s). Re-run 'vendors' to continue.");
    }

    List<VendorItem> vendorItems = byItem.Values.OrderBy(v => v.Item, StringComparer.OrdinalIgnoreCase).ToList();
    string vendorPath = Path.Combine(AppContext.BaseDirectory, "vendor-items.json");
    string vendorJson = JsonSerializer.Serialize(
        new
        {
            source = "https://wiki.guildwars2.com",
            note = "Every item sold by a vendor, keyed by GW2 item id. Cost may be coin or a currency, possibly multi-component.",
            count = vendorItems.Count,
            items = vendorItems,
        },
        new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(vendorPath, vendorJson);
    Console.Error.WriteLine($"Wrote {vendorItems.Count} vendor-sold item(s) to {vendorPath}");
}

// Fetch one partition to completion; if it hits SMW's offset cap, sub-split it by the sold item's rarity
// (one level) and fetch each leaf. De-dup across the partial parent fetch and the leaves handles overlap.
async Task FetchPartitionAsync(
    string label, string baseCondition, Dictionary<string, VendorItem> byItem, HashSet<string> seen, bool canSplit)
{
    const string OfferPrintouts = "|?Sells item|?Sells item.Has game id|?Has vendor|?Has item cost|?Has item quantity";
    string[] rarities = ["Junk", "Basic", "Fine", "Masterwork", "Rare", "Exotic", "Ascended", "Legendary"];

    (int count, bool capped) = await ForEachOfferAsync(baseCondition + OfferPrintouts, po => AddOffer(po, byItem, seen));

    if (capped && canSplit)
    {
        Console.Error.WriteLine($"  [{label}] {count} offer(s) — hit cap, splitting by rarity…");
        foreach (string rarity in rarities)
        {
            await FetchPartitionAsync(
                $"{label}/{rarity}",
                $"{baseCondition}[[Sells item.Has item rarity::{rarity}]]",
                byItem,
                seen,
                canSplit: false);
        }
    }
    else
    {
        Console.Error.WriteLine(
            $"  [{label}] {count} offer(s){(capped ? "  *** STILL CAPPED — INCOMPLETE ***" : "")}; {byItem.Count} item(s) total");
    }
}

// Page through an `ask` query's vendor-offer subobjects, invoking `handle` with each offer's printouts.
// Returns the offer count and whether it likely hit SMW's offset cap (continue-offset stopped advancing
// while a full page was still being returned — i.e. more rows exist than the wiki will hand out).
async Task<(int Count, bool Capped)> ForEachOfferAsync(string queryBody, Action<JsonElement> handle)
{
    int offset = 0;
    int count = 0;
    bool capped = false;
    while (true)
    {
        string url = $"{ApiUrl}?action=ask&format=json&query={Uri.EscapeDataString($"{queryBody}|limit=500|offset={offset}")}";
        using JsonDocument doc = JsonDocument.Parse(await GetWithRetryAsync(url));
        if (!doc.RootElement.TryGetProperty("query", out JsonElement queryNode)
            || !queryNode.TryGetProperty("results", out JsonElement resultsNode))
        {
            string detail = doc.RootElement.TryGetProperty("error", out JsonElement err) ? err.ToString() : "(no query node)";
            Console.Error.WriteLine($"  [warn] query returned no results node; stopping partition. {detail}");
            break;
        }

        // Results are an object (keyed by subobject id) when matched, an empty array [] when not.
        IEnumerable<JsonElement> offers = resultsNode.ValueKind == JsonValueKind.Object
            ? resultsNode.EnumerateObject().Select(p => p.Value)
            : resultsNode.EnumerateArray();
        int page = 0;
        foreach (JsonElement offer in offers)
        {
            if (offer.TryGetProperty("printouts", out JsonElement po))
            {
                handle(po);
            }

            page++;
            count++;
        }

        if (doc.RootElement.TryGetProperty("query-continue-offset", out JsonElement off)
            && off.ValueKind == JsonValueKind.Number)
        {
            if (off.GetInt32() > offset)
            {
                offset = off.GetInt32();
            }
            else
            {
                capped = page >= 500; // continue requested but offset won't advance → the wiki's cap
                break;
            }
        }
        else
        {
            break; // no continuation → clean end
        }

        await Task.Delay(1000);
    }

    return (count, capped);
}

static List<CostComponent> ReadCost(JsonElement printouts)
{
    var cost = new List<CostComponent>();
    if (printouts.TryGetProperty("Has item cost", out JsonElement costs) && costs.ValueKind == JsonValueKind.Array)
    {
        foreach (JsonElement c in costs.EnumerateArray())
        {
            if (RecordItem(c, "Has item currency") is { } currency
                && int.TryParse(RecordItem(c, "Has item value"), out int value))
            {
                cost.Add(new CostComponent(value, currency));
            }
        }
    }

    return cost;
}

static void AddOffer(JsonElement po, Dictionary<string, VendorItem> byItem, HashSet<string> seenOffers)
{
    if (FirstFulltext(po, "Sells item") is not { } item)
    {
        return;
    }

    List<CostComponent> cost = ReadCost(po);
    string vendor = FirstFulltext(po, "Has vendor") ?? "";
    int quantity = FirstInt(po, "Has item quantity") ?? 1;

    // A multi-currency offer appears once per cost-currency partition; collapse those repeats.
    string key = $"{item}{vendor}{quantity}{string.Join('+', cost.Select(c => $"{c.Value} {c.Currency}"))}";
    if (!seenOffers.Add(key))
    {
        return;
    }

    if (!byItem.TryGetValue(item, out VendorItem? vi))
    {
        // The chained ?Sells item.Has game id printout lands under the key "Has game id".
        vi = new VendorItem(FirstInt(po, "Has game id"), item, []);
        byItem[item] = vi;
    }

    vi.Offers.Add(new VendorOffer(vendor, quantity, cost));
}

static string? FirstFulltext(JsonElement printouts, string property)
{
    if (printouts.TryGetProperty(property, out JsonElement arr) && arr.ValueKind == JsonValueKind.Array
        && arr.GetArrayLength() > 0 && arr[0].TryGetProperty("fulltext", out JsonElement ft))
    {
        return ft.GetString();
    }

    return null;
}

static int? FirstInt(JsonElement printouts, string property)
{
    if (printouts.TryGetProperty(property, out JsonElement arr) && arr.ValueKind == JsonValueKind.Array
        && arr.GetArrayLength() > 0)
    {
        JsonElement e = arr[0];
        if (e.ValueKind == JsonValueKind.Number)
        {
            return e.GetInt32();
        }

        if (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out int n))
        {
            return n;
        }
    }

    return null;
}

// A record-typed SMW value: { "<sub>": { "item": ["<value>"] } }.
static string? RecordItem(JsonElement record, string subProperty)
{
    if (record.TryGetProperty(subProperty, out JsonElement s) && s.TryGetProperty("item", out JsonElement items)
        && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
    {
        return items[0].GetString();
    }

    return null;
}

// The wiki sits behind a CDN that 403/429s bursty automated traffic, so every request retries with
// exponential backoff. A request can't be re-sent, so callers pass a factory we re-invoke per attempt.
async Task<string> SendWithRetryAsync(Func<HttpRequestMessage> makeRequest)
{
    for (int attempt = 1; ; attempt++)
    {
        try
        {
            using HttpRequestMessage req = makeRequest();
            using HttpResponseMessage resp = await http.SendAsync(req);
            int code = (int)resp.StatusCode;
            if ((code == 403 || code == 429 || code >= 500) && attempt < 6)
            {
                int waitMs = 1000 * (int)Math.Pow(2, attempt); // 2s, 4s, 8s, 16s, 32s
                Console.Error.WriteLine($"  HTTP {code}; backing off {waitMs}ms (attempt {attempt})…");
                await Task.Delay(waitMs);
                continue;
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException) when (attempt < 6)
        {
            await Task.Delay(1000 * (int)Math.Pow(2, attempt));
        }
    }
}

// maxlag asks the wiki to shed our load first if its database replicas fall behind.
Task<string> GetWithRetryAsync(string url) =>
    SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url + "&maxlag=5"));

async Task<List<string>> ListCategoryPagesAsync(string category)
{
    var result = new List<string>();
    string? cont = null;
    do
    {
        string url = $"{ApiUrl}?action=query&list=categorymembers&format=json&formatversion=2"
            + $"&cmtitle={Uri.EscapeDataString(category)}&cmtype=page&cmlimit=500"
            + (cont is null ? "" : $"&cmcontinue={Uri.EscapeDataString(cont)}");

        using JsonDocument doc = JsonDocument.Parse(await GetWithRetryAsync(url));
        foreach (JsonElement m in doc.RootElement.GetProperty("query").GetProperty("categorymembers").EnumerateArray())
        {
            if (m.TryGetProperty("title", out JsonElement t) && t.GetString() is { } title)
            {
                result.Add(title);
            }
        }

        cont = doc.RootElement.TryGetProperty("continue", out JsonElement c)
               && c.TryGetProperty("cmcontinue", out JsonElement cc)
            ? cc.GetString()
            : null;
        if (cont is not null)
        {
            await Task.Delay(600);
        }
    } while (cont is not null);

    return result;
}

async Task<List<(string Title, string Wikitext)>> FetchWikitextAsync(List<string> batch)
{
    // Special:Export returns the current wikitext of all requested pages as one MediaWiki XML document.
    string xml = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Post, ExportUrl)
    {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["pages"] = string.Join("\n", batch),
            ["curonly"] = "1",
            ["action"] = "submit",
        }),
    });

    var result = new List<(string, string)>();
    XDocument doc = XDocument.Parse(xml);
    foreach (XElement page in doc.Descendants().Where(e => e.Name.LocalName == "page"))
    {
        string? title = page.Elements().FirstOrDefault(e => e.Name.LocalName == "title")?.Value;
        string? text = page.Descendants().FirstOrDefault(e => e.Name.LocalName == "text")?.Value;
        if (title is not null && text is not null)
        {
            result.Add((title, text));
        }
    }

    return result;
}

// A page may hold several {{recipe}} blocks (different sources/disciplines); keep only Mystic Forge ones.
static IEnumerable<Recipe> ParseMysticForgeRecipes(string title, string wikitext)
{
    foreach (string block in ExtractTemplates(wikitext, "recipe"))
    {
        Dictionary<string, string> f = ParseTemplateFields(block);
        if (!f.TryGetValue("source", out string? source) || !IsMysticForge(CleanWiki(source).Trim()))
        {
            continue;
        }

        string nameField = f.GetValueOrDefault("name") ?? "";
        string output = FirstNonEmpty(f.GetValueOrDefault("link"), f.GetValueOrDefault("result"), StripLeadingQty(nameField));
        if (string.IsNullOrWhiteSpace(output))
        {
            // On a dedicated item page the recipe omits its output name (it defaults to the page itself);
            // fall back to the page title, dropping any "/Recipes"-style subpage suffix.
            output = title.Split('/')[0];
        }

        var ingredients = new List<Ingredient>();
        for (int n = 1; f.TryGetValue($"ingredient{n}", out string? ing); n++)
        {
            if (string.IsNullOrWhiteSpace(ing))
            {
                continue;
            }

            (int count, string ingName) = SplitQtyName(ing);
            if (!string.IsNullOrWhiteSpace(ingName))
            {
                ingredients.Add(new Ingredient(count, ingName));
            }
        }

        if (ingredients.Count == 0)
        {
            continue;
        }

        // The wiki sometimes spreads one ingredient across several slots (e.g. "3 Crest" ×3); fold same-named
        // entries into a single summed quantity so consumers can cost the recipe directly.
        List<Ingredient> merged = ingredients
            .GroupBy(ing => ing.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Ingredient(g.Sum(x => x.Count), g.First().Name))
            .ToList();

        yield return new Recipe(CleanWiki(output).Trim(), LeadingInt(nameField) ?? 1, merged, title);
    }
}

// Returns the inner-to-outer text of each {{<name> ...}} invocation, brace-matched (templates nest).
static IEnumerable<string> ExtractTemplates(string text, string name)
{
    var opener = new Regex(@"\{\{\s*" + Regex.Escape(name) + @"\s*(?=[|}])", RegexOptions.IgnoreCase);
    int from = 0;
    while (true)
    {
        Match m = opener.Match(text, from);
        if (!m.Success)
        {
            yield break;
        }

        int start = m.Index;
        int depth = 0;
        int j = start;
        for (; j < text.Length - 1; j++)
        {
            if (text[j] == '{' && text[j + 1] == '{')
            {
                depth++;
                j++;
            }
            else if (text[j] == '}' && text[j + 1] == '}')
            {
                depth--;
                j++;
                if (depth == 0)
                {
                    j++;
                    break;
                }
            }
        }

        yield return text.Substring(start, Math.Min(j, text.Length) - start);
        from = j;
    }
}

static Dictionary<string, string> ParseTemplateFields(string block)
{
    string inner = block.Trim();
    if (inner.StartsWith("{{"))
    {
        inner = inner[2..];
    }

    if (inner.EndsWith("}}"))
    {
        inner = inner[..^2];
    }

    var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    List<string> parts = SplitTopLevel(inner, '|');
    foreach (string part in parts.Skip(1)) // parts[0] is the template name
    {
        int eq = part.IndexOf('=');
        if (eq < 0)
        {
            continue;
        }

        fields[part[..eq].Trim()] = part[(eq + 1)..].Trim();
    }

    return fields;
}

// Split on `sep` only at the top level — ignoring separators nested in {{...}} or [[...]].
static List<string> SplitTopLevel(string s, char sep)
{
    var result = new List<string>();
    int curly = 0;
    int square = 0;
    int last = 0;
    for (int k = 0; k < s.Length; k++)
    {
        if (k < s.Length - 1 && s[k] == '{' && s[k + 1] == '{')
        {
            curly++;
            k++;
        }
        else if (k < s.Length - 1 && s[k] == '}' && s[k + 1] == '}')
        {
            curly = Math.Max(0, curly - 1);
            k++;
        }
        else if (k < s.Length - 1 && s[k] == '[' && s[k + 1] == '[')
        {
            square++;
            k++;
        }
        else if (k < s.Length - 1 && s[k] == ']' && s[k + 1] == ']')
        {
            square = Math.Max(0, square - 1);
            k++;
        }
        else if (s[k] == sep && curly == 0 && square == 0)
        {
            result.Add(s[last..k]);
            last = k + 1;
        }
    }

    result.Add(s[last..]);
    return result;
}

// "6 Philosopher's Stone" -> (6, "Philosopher's Stone"); "Mystic Coin" -> (1, "Mystic Coin").
static (int Count, string Name) SplitQtyName(string raw)
{
    string s = CleanWiki(raw).Trim();
    Match m = Regex.Match(s, @"^(\d+)\s+(.+)$");
    return m.Success ? (int.Parse(m.Groups[1].Value), m.Groups[2].Value.Trim()) : (1, s);
}

static int? LeadingInt(string s)
{
    Match m = Regex.Match(CleanWiki(s).Trim(), @"^(\d+)");
    return m.Success ? int.Parse(m.Groups[1].Value) : null;
}

static string StripLeadingQty(string s) => Regex.Replace(CleanWiki(s).Trim(), @"^\d+\s+", "");

// [[target|display]] -> display, [[target]] -> target; leaves plain text untouched.
static string CleanWiki(string s)
{
    s = Regex.Replace(s, @"\[\[([^\]|]+)\|([^\]]+)\]\]", "$2");
    s = Regex.Replace(s, @"\[\[([^\]]+)\]\]", "$1");
    return s;
}

static string FirstNonEmpty(params string?[] values) =>
    values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

// The {{recipe}} template writes the Mystic Forge source as either "Mystic Forge" or the shorthand
// "mystic" (used on infused ascended trinkets, which are forged). Both mean the Mystic Forge.
static bool IsMysticForge(string source) =>
    source.Equals("Mystic Forge", StringComparison.OrdinalIgnoreCase)
    || source.Equals("mystic", StringComparison.OrdinalIgnoreCase);

internal sealed record Recipe(string Output, int OutputCount, List<Ingredient> Ingredients, string SourcePage);

internal sealed record Ingredient(int Count, string Name);

internal sealed record VendorItem(int? GameId, string Item, List<VendorOffer> Offers);

internal sealed record VendorOffer(string Vendor, int Quantity, List<CostComponent> Cost);

internal sealed record CostComponent(int Value, string Currency);
