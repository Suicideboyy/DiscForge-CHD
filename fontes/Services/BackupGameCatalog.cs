using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Serial is authoritative. Title matching is exact after removing a known translation label;
// ambiguous entries are never used to guess a different game or console generation.
static class BackupGameCatalog
{
    internal sealed record Entry(string Name, string Serial);
    static readonly object Gate = new();
    static readonly Dictionary<bool, Task<Entry[]>> Pending = new();

    public static string CacheRoot => Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHD Optimizer");

    public static string Url(bool ps2) => "https://raw.githubusercontent.com/libretro/libretro-database/"
        + "master/metadat/redump/" + Uri.EscapeDataString(ps2 ? "Sony - PlayStation 2.dat"
            : "Sony - PlayStation.dat");

    public static Task<Entry[]> LoadAsync(bool ps2)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(ps2, out var pending))
                Pending[ps2] = pending = LoadCoreAsync(ps2);
            return pending;
        }
    }

    static async Task<Entry[]> LoadCoreAsync(bool ps2)
    {
        int limit = ps2 ? 24000000 : 12000000;
        string folder = Path.Combine(CacheRoot, "metadata");
        string file = Path.Combine(folder, ps2 ? "libretro-ps2.dat" : "libretro-ps1.dat");
        try
        {
            byte[] data;
            if (File.Exists(file) && new FileInfo(file).Length <= limit
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < TimeSpan.FromDays(30))
                data = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
            else
            {
                data = await HttpData.DownloadAsync(Url(ps2), limit).ConfigureAwait(false);
                try
                {
                    Directory.CreateDirectory(folder);
                    await File.WriteAllBytesAsync(file, data).ConfigureAwait(false);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return Parse(Encoding.UTF8.GetString(data));
        }
        catch { return []; }
    }

    internal static Entry[] Parse(string text)
    {
        var records = new List<Entry>();
        string name = "";
        using var reader = new StringReader(text);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("game (", StringComparison.Ordinal)) name = "";
            else if (line.StartsWith("name \"", StringComparison.Ordinal))
                name = Regex.Match(line, "^name \"([^\"]+)\"").Groups[1].Value;
            else if (name.Length > 0 && line.StartsWith("serial \"", StringComparison.Ordinal))
                foreach (Match serial in Regex.Matches(line, @"\b[A-Z]{4}-\d{5}\b"))
                    records.Add(new Entry(name, serial.Value));
        }
        return records.Distinct().ToArray();
    }

    public static string CleanTitle(string title)
    {
        string leaf = Path.GetFileName(title ?? "");
        leaf = Regex.Replace(leaf, @"(?i)\.(?:zip|rar|7z|chd|iso|bin|cue|img|gz|bz2|xz)$", "");
        return Regex.Replace(leaf, @"(?i)^\[(?:[a-z]{2}(?:\s+[\d.]+)?|T-En[^\]]*)\]\s*", "").Trim();
    }

    static string Key(string text) => new string(text.Normalize(NormalizationForm.FormKC)
        .Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    internal static Entry[] Match(Entry[] records, string serial, string title)
    {
        if (Regex.IsMatch(serial ?? "", @"^[A-Z]{4}-\d{5}$"))
            return records.Where(e => e.Serial == serial).Distinct().ToArray();
        string key = Key(CleanTitle(title));
        if (key.Length == 0) return [];
        var matches = records.Where(e => Key(e.Name) == key).ToArray();
        return matches.Select(e => e.Name).Distinct().Count() == 1 ? matches : [];
    }

    public static async Task<string[]> NamesAsync(string serial, string title, bool ps2)
    {
        var matches = Match(await LoadAsync(ps2).ConfigureAwait(false), serial, title);
        var names = matches.Select(e => e.Name).Distinct().Take(3).ToList();
        string clean = CleanTitle(title);
        if (names.Any(n => Key(n) == Key(clean)) && !names.Contains(clean)) names.Add(clean);
        return names.ToArray();
    }
}
