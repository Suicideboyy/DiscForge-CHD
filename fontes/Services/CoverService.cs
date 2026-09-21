using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Drawing;

static class CoverService
{
    public static async Task<Image> Load(string serial)
    {
        if (!Regex.IsMatch(serial ?? "", @"^[A-Z]{4}-\d{5}$"))
        {
            return null;
        }

        return await Task.Run(() =>
        {
            string root = Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE");
            if (String.IsNullOrEmpty(root))
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CHD Optimizer");
            }

            string folder = Path.Combine(root, "covers"), file = Path.Combine(folder, serial + ".jpg");
            try
            {
                byte[] data = null;
                if (File.Exists(file))
                {
                    try
                    {
                        data = File.ReadAllBytes(file);
                        using (var ms = new MemoryStream(data))
                            using (var img = Image.FromStream(ms))
                            {
                                return (Image)new Bitmap(img);
                            }
                    }
                    catch
                    {
                        data = null;
                    }
                }

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                const string coverBaseUrl =
                    "https://raw.githubusercontent.com/xlenore/ps2-covers/main/covers/default/";
                var request = (HttpWebRequest)WebRequest.Create(coverBaseUrl + serial + ".jpg");
                request.Timeout = 12000;
                request.ReadWriteTimeout = 12000;
                request.UserAgent = AppInfo.UserAgent;
                using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                        using (var ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[8192];
                            int n;
                            while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, n);
                                if (ms.Length > 10000000)
                                {
                                    throw new IOException("Capa muito grande");
                                }
                            }

                            data = ms.ToArray();
                        }

                using (var ms = new MemoryStream(data))
                    using (var img = Image.FromStream(ms))
                    {
                        var copy = new Bitmap(img);
                        try
                        {
                            Directory.CreateDirectory(folder);
                            File.WriteAllBytes(file, data);
                        }
                        catch
                        {
                        }

                        return (Image)copy;
                    }
            }
            catch
            {
                return null;
            }
        }

        ).ConfigureAwait(false);
    }
}
