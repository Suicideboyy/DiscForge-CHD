
sealed record CoverImage(byte[] Bytes, string Provider, string Url);

static class CoverService
{
    const int Limit = 10000000;
    static readonly object Gate = new();
    static readonly Dictionary<string, DateTime> Misses = new();

    public static async Task<byte[]> Load(string serial, bool ps2 = true) =>
        (await LoadDetails(serial, "", ps2).ConfigureAwait(false))?.Bytes;

    // Keep the serial-based provider first, then use confirmed catalog titles for box art.
    public static async Task<CoverImage> LoadDetails(string serial, string title, bool ps2,
        string source = "")
    {
        string key = (ps2 ? "PS2|" : "PS1|") + serial + "|" + title + "|" + source;
        lock (Gate)
            if (Misses.TryGetValue(key, out var missed) && DateTime.UtcNow - missed < TimeSpan.FromMinutes(30))
                return null;
        string folder = Path.Combine(BackupGameCatalog.CacheRoot, "covers", ps2 ? "ps2" : "ps1");
        try
        {
            if (Regex.IsMatch(serial ?? "", @"^[A-Z]{4}-\d{5}$"))
            {
                string url = ps2
                    ? "https://raw.githubusercontent.com/xlenore/ps2-covers/main/covers/default/" + serial + ".jpg"
                    : "https://raw.githubusercontent.com/xlenore/psx-covers/main/covers/3d/" + serial + ".png";
                var primary = await TryImage(folder, serial + (ps2 ? ".jpg" : ".png"), url,
                    ps2 ? "xlenore/ps2-covers" : "xlenore/psx-covers").ConfigureAwait(false);
                if (primary != null) return primary;
            }

            var names = (await BackupGameCatalog.NamesAsync(serial, source.Length > 0 ? source : title,
                ps2).ConfigureAwait(false)).ToList();
            foreach (string name in names.ToArray())
            {
                string single = Regex.Replace(name, @"\s*\(Disc \d+\)", "");
                if (single != name && !names.Contains(single)) names.Add(single);
            }
            string repository = ps2 ? "Sony_-_PlayStation_2" : "Sony_-_PlayStation";
            foreach (string name in names.Take(6))
            {
                string url = "https://raw.githubusercontent.com/libretro-thumbnails/" + repository
                    + "/master/Named_Boxarts/" + Uri.EscapeDataString(name.Replace('&', '_') + ".png");
                string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)));
                var backup = await TryImage(folder, "libretro-" + hash + ".png", url,
                    "libretro-thumbnails/" + repository).ConfigureAwait(false);
                if (backup != null) return backup;
            }
        }
        catch { }
        lock (Gate)
        {
            if (Misses.Count >= 1024) Misses.Clear();
            Misses[key] = DateTime.UtcNow;
        }
        return null;
    }

    static async Task<CoverImage> TryImage(string folder, string name, string url, string provider)
    {
        try
        {
            string file = Path.Combine(folder, name);
            if (File.Exists(file) && new FileInfo(file).Length <= Limit)
            {
                byte[] cached = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
                if (IsImage(cached)) return new CoverImage(cached, provider, url);
            }
            byte[] data = await HttpData.DownloadAsync(url, Limit).ConfigureAwait(false);
            if (!IsImage(data)) return null;
            try
            {
                Directory.CreateDirectory(folder);
                await File.WriteAllBytesAsync(file, data).ConfigureAwait(false);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return new CoverImage(data, provider, url);
        }
        catch { return null; }
    }

    public static bool IsImage(byte[] data) => data != null && (data.Length > 3
        && data[0] == 255 && data[1] == 216 && data[2] == 255 || data.Length > 8
        && data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71
        && data[4] == 13 && data[5] == 10 && data[6] == 26 && data[7] == 10);
}
