using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static class CoverService
{
    public static async Task<byte[]> Load(string serial)
    {
        if (!Regex.IsMatch(serial ?? "", @"^[A-Z]{4}-\d{5}$"))
            return null;
        string root = Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CHD Optimizer");
        string folder = Path.Combine(root, "covers");
        string file = Path.Combine(folder, serial + ".jpg");
        try
        {
            if (File.Exists(file) && new FileInfo(file).Length <= 10000000)
            {
                byte[] cached = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
                if (IsJpeg(cached))
                    return cached;
            }
            byte[] data = await HttpData.DownloadAsync(
                "https://raw.githubusercontent.com/xlenore/ps2-covers/main/covers/default/" + serial + ".jpg",
                10000000).ConfigureAwait(false);
            if (!IsJpeg(data))
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

    static bool IsJpeg(byte[] data) => data.Length > 3 && data[0] == 255 && data[1] == 216 && data[2] == 255;
}
