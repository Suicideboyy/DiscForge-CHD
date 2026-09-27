using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static class CoverService
{
    public static async Task<byte[]> Load(string serial, bool ps2 = true)
    {
        if (!Regex.IsMatch(serial ?? "", @"^[A-Z]{4}-\d{5}$"))
            return null;
        string root = Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CHD Optimizer");
        string folder = Path.Combine(root, "covers", ps2 ? "ps2" : "ps1");
        string file = Path.Combine(folder, serial + (ps2 ? ".jpg" : ".png"));
        try
        {
            if (File.Exists(file) && new FileInfo(file).Length <= 10000000)
            {
                byte[] cached = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
                if (IsImage(cached, ps2))
                    return cached;
            }
            string url = ps2
                ? "https://raw.githubusercontent.com/xlenore/ps2-covers/main/covers/default/" + serial + ".jpg"
                : "https://raw.githubusercontent.com/xlenore/psx-covers/main/covers/3d/" + serial + ".png";
            byte[] data = await HttpData.DownloadAsync(
                url,
                10000000).ConfigureAwait(false);
            if (!IsImage(data, ps2))
                return null;
            try
            {
                Directory.CreateDirectory(folder);
                await File.WriteAllBytesAsync(file, data).ConfigureAwait(false);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return data;
        }
        catch { return null; }
    }

    static bool IsImage(byte[] data, bool ps2) => ps2
        ? data.Length > 3 && data[0] == 255 && data[1] == 216 && data[2] == 255
        : data.Length > 8 && data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71
            && data[4] == 13 && data[5] == 10 && data[6] == 26 && data[7] == 10;
}
