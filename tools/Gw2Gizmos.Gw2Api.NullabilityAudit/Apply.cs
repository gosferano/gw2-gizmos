using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Gw2Gizmos.Gw2Api.NullabilityAudit;

// Maps the empirical report's optional field paths onto the exact DTO properties and makes the
// non-collection ones nullable. Dry-run by default; pass --write to edit the .cs files.
internal static class Apply
{
    // endpoint -> root DTO type (relative to the Contract namespace).
    private static readonly Dictionary<string, string> EndpointType = new()
    {
        ["items"] = "V2.Items.Item",
        ["skills"] = "V2.Skills.Skill",
        ["recipes"] = "V2.Recipes.Recipe",
        ["traits"] = "V2.Traits.Trait",
        ["skins"] = "V2.Skins.Skin",
        ["colors"] = "V2.Colors.Color",
        ["currencies"] = "V2.Currencies.Currency",
        ["minis"] = "V2.Minis.Mini",
        ["materials"] = "V2.Materials.MaterialCategory",
        ["itemstats"] = "V2.ItemStats.ItemStat",
        ["achievements"] = "V2.Achievements.Achievement",
        ["achievements/categories"] = "V2.Achievements.AchievementCategory",
        ["achievements/groups"] = "V2.Achievements.AchievementGroup",
        ["titles"] = "V2.Titles.Title",
        ["masteries"] = "V2.Masteries.Mastery",
        ["mounts/skins"] = "V2.Mounts.MountSkin",
        ["mounts/types"] = "V2.Mounts.MountType",
        ["novelties"] = "V2.Novelties.Novelty",
        ["outfits"] = "V2.Outfits.Outfit",
        ["pets"] = "V2.Pets.Pet",
        ["professions"] = "V2.Professions.Profession",
        ["races"] = "V2.Races.Race",
        ["specializations"] = "V2.Specializations.Specialization",
        ["finishers"] = "V2.Finishers.Finisher",
        ["gliders"] = "V2.Gliders.Glider",
        ["jadebots"] = "V2.JadeBots.JadeBot",
        ["legends"] = "V2.Legends.Legend",
        ["mailcarriers"] = "V2.MailCarriers.MailCarrier",
        ["maps"] = "V2.Maps.Map",
        ["quests"] = "V2.Quests.Quest",
        ["raids"] = "V2.Raids.Raid",
        ["dungeons"] = "V2.Dungeons.Dungeon",
        ["emotes"] = "V2.Emotes.Emote",
        ["worldbosses"] = "V2.WorldBosses.WorldBoss",
        ["worlds"] = "V2.Worlds.World",
        ["backstory/answers"] = "V2.Backstory.BackstoryAnswer",
        ["backstory/questions"] = "V2.Backstory.BackstoryQuestion",
        ["stories"] = "V2.Stories.Story",
        ["stories/seasons"] = "V2.Stories.StorySeason",
        ["pvp/amulets"] = "V2.Pvp.PvpAmulet",
        ["pvp/heroes"] = "V2.Pvp.PvpHero",
        ["pvp/ranks"] = "V2.Pvp.PvpRank",
        ["wvw/abilities"] = "V2.Wvw.WvwAbility",
        ["wvw/objectives"] = "V2.Wvw.WvwObjective",
        ["wvw/ranks"] = "V2.Wvw.WvwRank",
        ["wvw/upgrades"] = "V2.Wvw.WvwUpgrade",
        ["guild/upgrades"] = "V2.Guild.GuildUpgrade",
        ["guild/permissions"] = "V2.Guild.GuildPermission",
        // Token-gated (sampled with GW2_API_KEY). Array endpoints map to their element type.
        ["account"] = "V2.Account.Account",
        ["account/achievements"] = "V2.Account.AccountAchievement",
        ["account/bank"] = "V2.Account.AccountItem",
        ["account/inventory"] = "V2.Account.AccountItem",
        ["account/materials"] = "V2.Account.AccountMaterial",
        ["account/wallet"] = "V2.Account.AccountWalletCurrency",
        ["characters"] = "V2.Characters.Character",
    };

    public static void Run(string[] args)
    {
        bool write = args.Contains("--write");
        Assembly asm = typeof(Gw2Gizmos.Gw2Api.Contract.V2.Items.Item).Assembly;
        const string ns = "Gw2Gizmos.Gw2Api.Contract.";
        Type[] all = asm.GetTypes();

        string reportPath = Path.Combine(AppContext.BaseDirectory, "nullability-report.json");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));

        var targets = new HashSet<(Type type, string prop)>();
        var unmapped = new List<string>();

        foreach (JsonProperty ep in doc.RootElement.EnumerateObject())
        {
            if (!ep.Value.TryGetProperty("optionalFields", out JsonElement fields))
            {
                continue;
            }

            if (!EndpointType.TryGetValue(ep.Name, out string? typeName))
            {
                unmapped.Add(ep.Name);
                continue;
            }

            Type? root = asm.GetType(ns + typeName);
            if (root is null)
            {
                unmapped.Add($"{ep.Name} (type not found: {typeName})");
                continue;
            }

            foreach (JsonElement field in fields.EnumerateArray())
            {
                string[] segs = field.GetProperty("field").GetString()!.Split('.');
                foreach (PropertyInfo leaf in ResolveLeaves(root, segs, 0, all))
                {
                    if (!IsCollection(leaf.PropertyType)) // collections are empty-initialized; leave them
                    {
                        targets.Add((leaf.DeclaringType!, leaf.Name));
                    }
                }
            }
        }

        string contractSrc = FindContractSrc();
        Dictionary<string, string> typeFile = MapTypesToFiles(contractSrc);

        var byFile = new SortedDictionary<string, List<string>>();
        int changed = 0;
        foreach ((Type type, string prop) in targets.OrderBy(t => t.type.Name).ThenBy(t => t.prop))
        {
            if (!typeFile.TryGetValue(type.Name, out string? file))
            {
                Console.Error.WriteLine($"  ! no source file for {type.Name}");
                continue;
            }

            string? before = EditProperty(file, prop, write, out string? after);
            if (before is null)
            {
                continue; // not found or already nullable
            }

            byFile.TryAdd(file, new List<string>());
            byFile[file].Add($"    {type.Name}.{prop}:  {before.Trim()}  ->  {after!.Trim()}");
            changed++;
        }

        foreach ((string file, List<string> edits) in byFile)
        {
            Console.WriteLine(Path.GetRelativePath(contractSrc, file));
            foreach (string e in edits)
            {
                Console.WriteLine(e);
            }
        }

        Console.WriteLine($"\n{(write ? "APPLIED" : "DRY-RUN")}: {changed} properties across {byFile.Count} files.");
        if (unmapped.Count > 0)
        {
            Console.WriteLine($"Unmapped endpoints (no type): {string.Join(", ", unmapped)}");
        }
    }

    // Finds the property/properties a dotted JSON path resolves to. Each segment may carry a
    // discriminator marker ("name[Disc]", or a leading "[Disc]" for the root object) that narrows the
    // search to the matching polymorphic subtype; an unmatched marker (e.g. a value-level discriminator
    // like an armor slot) is ignored and the current type is kept.
    private static IEnumerable<PropertyInfo> ResolveLeaves(Type type, string[] path, int idx, Type[] all)
    {
        (string name, string? disc) = ParseSeg(path[idx]);

        if (name.Length == 0)
        {
            // Pure discriminator segment (leading "[Armor]"): narrow `type`, then advance.
            foreach (Type c in NarrowByDisc(type, disc, all))
            {
                foreach (PropertyInfo r in ResolveLeaves(c, path, idx + 1, all))
                {
                    yield return r;
                }
            }

            yield break;
        }

        string pascal = SnakeToPascal(name);
        foreach (Type t in TypesToSearch(type, all))
        {
            PropertyInfo? prop = t.GetProperty(
                pascal,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly
            );
            if (prop is null)
            {
                continue;
            }

            if (idx == path.Length - 1)
            {
                yield return prop;
            }
            else
            {
                // Descend into the property's element type, narrowed by this segment's discriminator.
                foreach (Type c in NarrowByDisc(ElementType(prop.PropertyType), disc, all))
                {
                    foreach (PropertyInfo r in ResolveLeaves(c, path, idx + 1, all))
                    {
                        yield return r;
                    }
                }
            }
        }
    }

    private static (string name, string? disc) ParseSeg(string seg)
    {
        int b = seg.IndexOf('[');
        if (b < 0)
        {
            return (seg, null);
        }

        int e = seg.IndexOf(']', b);
        string disc = e > b ? seg[(b + 1)..e] : "";
        return (seg[..b], disc);
    }

    // Narrows `type` to the subtype matching a JSON discriminator value, by C# name. Prefers an exact
    // name match (Item subtypes: "Armor" -> Armor); else the shortest EndsWith match (SkillFact subtypes:
    // "Buff" -> SkillFactBuff, not SkillFactPrefixedBuff). No match -> the discriminator is value-level
    // (e.g. an armor slot on ArmorDetails), so the type is kept as-is.
    private static IEnumerable<Type> NarrowByDisc(Type type, string? disc, Type[] all)
    {
        if (string.IsNullOrEmpty(disc))
        {
            yield return type;
            yield break;
        }

        string p = SnakeToPascal(disc);
        Type[] family = all.Where(t => type.IsAssignableFrom(t)).ToArray();

        Type[] exact = family.Where(t => t.Name == p).ToArray();
        if (exact.Length > 0)
        {
            foreach (Type t in exact)
            {
                yield return t;
            }

            yield break;
        }

        Type[] ends = family.Where(t => t.Name.EndsWith(p, StringComparison.Ordinal)).ToArray();
        if (ends.Length > 0)
        {
            int min = ends.Min(t => t.Name.Length);
            foreach (Type t in ends.Where(t => t.Name.Length == min))
            {
                yield return t;
            }

            yield break;
        }

        yield return type;
    }

    private static IEnumerable<Type> TypesToSearch(Type type, Type[] all)
    {
        yield return type;
        foreach (Type t in all)
        {
            if (t != type && type.IsAssignableFrom(t)) // concrete + intermediate subtypes
            {
                yield return t;
            }
        }
    }

    private static Type ElementType(Type t)
    {
        if (t.IsArray)
        {
            return t.GetElementType()!;
        }

        if (t.IsGenericType)
        {
            foreach (Type arg in t.GetGenericArguments())
            {
                return arg; // first type arg of List<T>/IEnumerable<T>/etc.
            }
        }

        return t;
    }

    private static bool IsCollection(Type t) =>
        t != typeof(string) && (t.IsArray || typeof(IEnumerable).IsAssignableFrom(t));

    // Reads the file, makes the property nullable. Returns the original declaration line (or null if
    // not found / already nullable); `after` is the rewritten line.
    private static string? EditProperty(string file, string prop, bool write, out string? after)
    {
        after = null;
        string text = File.ReadAllText(file);
        string nl = text.Contains("\r\n") ? "\r\n" : "\n";
        string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

        var rx = new Regex($@"^(\s*public\s+)(\S.*?)(\s+{Regex.Escape(prop)}\s*\{{\s*get;\s*(?:set|init);\s*\}})(\s*=\s*[^;]+)?;?\s*$");
        for (int i = 0; i < lines.Length; i++)
        {
            Match m = rx.Match(lines[i]);
            if (!m.Success)
            {
                continue;
            }

            string type = m.Groups[2].Value;
            if (type.EndsWith('?'))
            {
                return null; // already nullable
            }

            string original = lines[i];
            lines[i] = $"{m.Groups[1].Value}{type}?{m.Groups[3].Value}";
            after = lines[i];

            if (write)
            {
                byte[] bytes = File.ReadAllBytes(file);
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                File.WriteAllText(file, string.Join(nl, lines), new UTF8Encoding(bom));
            }

            return original;
        }

        return null;
    }

    private static Dictionary<string, string> MapTypesToFiles(string contractSrc)
    {
        var map = new Dictionary<string, string>();
        var rx = new Regex(@"\bclass\s+(\w+)");
        foreach (string file in Directory.EnumerateFiles(contractSrc, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            foreach (Match m in rx.Matches(File.ReadAllText(file)))
            {
                map.TryAdd(m.Groups[1].Value, file);
            }
        }

        return map;
    }

    private static string FindContractSrc()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "src", "Gw2Gizmos.Gw2Api.Contract");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Gw2Gizmos.Gw2Api.Contract");
    }

    private static string SnakeToPascal(string snake) =>
        string.Concat(snake.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
}
