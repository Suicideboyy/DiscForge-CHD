using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

static class BundledTools
{
    public static string Tools;
    public static string Stage()
    {
        var asm = Assembly.GetExecutingAssembly();
        string id;
        using (var f = File.OpenRead(asm.Location))
            using (var sha = SHA256.Create())
            {
                id = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").Substring(0, 16);
            }

        string root = Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE");
        if (String.IsNullOrEmpty(root))
        {
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CHD Optimizer", "tools");
        }

        Tools = Path.Combine(root, id);
        Directory.CreateDirectory(Tools);
        FileSystemPaths.EnsureNoLinks(Tools);
        foreach (string name in new[]{"chdman.exe", "7z.exe", "7z.dll"})
        {
            string file = Path.Combine(Tools, name);
            FileSystemPaths.EnsureNoLinks(file);
            using (var resource = asm.GetManifestResourceStream(name))
                using (var memory = new MemoryStream())
                {
                    if (resource == null)
                    {
                        throw new IOException("Componente ausente: " + name);
                    }

                    resource.CopyTo(memory);
                    byte[] data = memory.ToArray();
                    bool same = false;
                    if (File.Exists(file))
                    {
                        using (var sha = SHA256.Create())
                            using (var old = File.OpenRead(file))
                            {
                                string existingHash = System.Convert.ToBase64String(sha.ComputeHash(old));
                                string bundledHash = System.Convert.ToBase64String(sha.ComputeHash(data));
                                same = existingHash == bundledHash;
                            }
                    }

                    if (!same)
                    {
                        File.WriteAllBytes(file, data);
                    }
                }
        }

        return Tools;
    }
}
