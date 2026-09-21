using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

sealed class GameDatabase
{
    readonly bool online;
    readonly string cacheDirectory;
    readonly Action<string> report;
    readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
    readonly Dictionary<string, GameRecord> cache = new Dictionary<string, GameRecord>();
    readonly HashSet<string> misses = new HashSet<string>();
    readonly Dictionary<string, int> failures = new Dictionary<string, int>();
    public GameDatabase(bool online, string cacheDirectory, Action<string> report)
    {
        this.online = online;
        this.cacheDirectory = cacheDirectory;
        this.report = report;
        LoadCache();
    }

    void LoadCache()
    {
        try
        {
            string p = Path.Combine(cacheDirectory, "ps2-media-cache.json");
            if (!File.Exists(p))
            {
                return;
            }

            foreach (var r in serializer.Deserialize<GameRecord[]>(File.ReadAllText(p)))
            {
                DateTime date;
                if (Regex.IsMatch(r.Serial ?? "", @"^[A-Z]{4}-\d{5}$") && (r.Type == "CD"
                    || r.Type == "DVD") && !String.IsNullOrWhiteSpace(r.Title) && (r.Provider == "Redump"
                    || r.Provider == "PSX Data Center") && DateTime.TryParse(r.Checked, out date)
                    && (DateTime.UtcNow - date.ToUniversalTime()).TotalDays < 180)
                {
                    cache[r.Serial] = r;
                }
            }
        }
        catch
        {
        }
    }

    void SaveCache()
    {
        string p = Path.Combine(cacheDirectory, "ps2-media-cache.json"), tmp = p + "."
            + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tmp, serializer.Serialize(cache.Values.ToArray()), Encoding.UTF8);
            if (File.Exists(p))
            {
                File.Replace(tmp, p, null);
            }
            else
            {
                File.Move(tmp, p);
            }
        }
        catch (Exception ex)
        {
            report("Aviso: cache de mídia: " + ex.Message);
        }
        finally
        {
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }
    }

    static string Plain(string html)
    {
        return Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, @"<[^>]*>", " ")), @"\s+", " ").Trim();
    }

    static List<string> Fields(string html, params string[] labels)
    {
        var values = new List<string>();
        foreach (Match row in Regex.Matches(html, @"(?is)<tr\b[^>]*>((?:(?!<tr\b).)*?)</tr>"))
        {
            var cells = Regex.Matches(row.Groups[1].Value, @"(?is)<t[dh]\b[^>]*>(.*?)</t[dh]>");
            if (cells.Count < 2 || !labels.Contains(Plain(cells[0].Groups[1].Value),
                StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            for (int i = 1; i < cells.Count; i++)
            {
                values.Add(Plain(cells[i].Groups[1].Value));
            }
        }

        return values;
    }

    public GameRecord Lookup(string serial)
    {
        if (serial.Length == 0)
        {
            return null;
        }

        GameRecord hit;
        if (cache.TryGetValue(serial, out hit))
        {
            return hit;
        }

        if (!online || misses.Contains(serial))
        {
            return null;
        }

        var providers = new[]{"Redump", "PSX Data Center"};
        var urls = new[]{"https://redump.info/discs?system=PS2&q=" + serial,
            "https://psxdatacenter.com/psx2/games2/" + serial + ".html"};
        for (int i = 0; i < urls.Length; i++)
        {
            int failed;
            failures.TryGetValue(providers[i], out failed);
            if (failed >= 2)
            {
                continue;
            }

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var request = (HttpWebRequest)WebRequest.Create(urls[i]);
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;
                request.MaximumAutomaticRedirections = 4;
                request.UserAgent = "CHD-Optimizer/1.2.1";
                string html;
                using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                        using (var reader = new StreamReader(stream))
                        {
                            var buffer = new char[8192];
                            var text = new StringBuilder();
                            int n;
                            while ((n = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                text.Append(buffer, 0, n);
                                if (text.Length > 8000000)
                                {
                                    throw new IOException("Página muito grande.");
                                }
                            }

                            html = text.ToString();
                        }

                failures[providers[i]] = 0;
                string serialFields = String.Join(" ", Fields(html, i == 0 ? new[]{"Disc Serial",
                    "Disc Serials", "Serial"} : new[]{"SERIAL NUMBER(S)"}));
                if (!Regex.IsMatch(serialFields.Replace('_', '-'), Regex.Escape(serial),
                    RegexOptions.IgnoreCase) && MediaFiles.NormalizeSerial(serialFields) != serial)
                {
                    continue;
                }

                if (i == 0 && !Fields(html, "System").Contains("Sony PlayStation 2"))
                {
                    continue;
                }

                var types = new HashSet<string>();
                foreach (string value in Fields(html, "MEDIA"))
                {
                    types.Add(Regex.IsMatch(value, @"(?i)^CD(?:-ROM)?$") ? "CD" : Regex.IsMatch(value,
                        @"(?i)^DVD(?:-[59]|-ROM)?$") ? "DVD" : "?");
                }

                if (types.Count != 1 || types.Contains("?"))
                {
                    continue;
                }

                string title = i == 1 ? String.Join(" ", Fields(html, "OFFICIAL TITLE"))
                    : Plain(Regex.Match(html,
                    @"(?is)<div\b[^>]*class=""disc-title-box""[^>]*>\s*<h2\b[^>]*>(.*?)</h2>").Groups[1].Value);
                hit = new GameRecord
                {
                    Serial = serial,
                    Type = types.First(),
                    Title = title,
                    Provider = providers[i],
                    Url = urls[i],
                    Checked = DateTime.UtcNow.ToString("o")
                };
                cache[serial] = hit;
                SaveCache();
                return hit;
            }
            catch
            {
                failures[providers[i]] = failed + 1;
            }
        }

        misses.Add(serial);
        return null;
    }
}
