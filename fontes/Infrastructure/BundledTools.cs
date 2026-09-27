using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

static class BundledTools
{
    public static string Tools;
    public static void ValidateSelectedTools(EncoderSettings settings)
    {
        Validate(settings.ChdmanPath, ".exe");
        Validate(settings.SevenZipExePath, ".exe");
        Validate(settings.SevenZipDllPath, ".dll");
    }

    static void Validate(string path, string extension)
    {
        if (String.IsNullOrWhiteSpace(path)) return;
        string full = Path.GetFullPath(path);
        if (!File.Exists(full) || !Path.GetExtension(full).Equals(extension, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Invalid tool path: " + path);
        FileSystemPaths.EnsureNoLinks(full);
    }

    // Copies selected tools to an isolated cache so 7z.exe can find 7z.dll beside it.
    public static string Stage(EncoderSettings settings = null)
    {
        if (settings != null) ValidateSelectedTools(settings);
        var asm = Assembly.GetExecutingAssembly();
        string id;
        using (var f = File.OpenRead(asm.Location))
            using (var sha = SHA256.Create())
            {
                id = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").Substring(0, 16);
            }

        string[] selected = settings == null
            ? new[] { "", "", "" }
            : new[] { settings.ChdmanPath, settings.SevenZipExePath, settings.SevenZipDllPath };
        using (var hash = SHA256.Create())
        using (var signature = new MemoryStream())
        {
            foreach (string path in selected)
            {
                if (String.IsNullOrWhiteSpace(path)) continue;
                using var input = File.OpenRead(path);
                byte[] digest = hash.ComputeHash(input);
                signature.Write(digest, 0, digest.Length);
            }
            signature.Position = 0;
            id += "-" + BitConverter.ToString(hash.ComputeHash(signature)).Replace("-", "").Substring(0, 12);
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
        string[] names = { "chdman.exe", "7z.exe", "7z.dll" };
        for (int index = 0; index < names.Length; index++)
        {
            string name = names[index];
            string file = Path.Combine(Tools, name);
            FileSystemPaths.EnsureNoLinks(file);
            using (var resource = String.IsNullOrWhiteSpace(selected[index])
                ? asm.GetManifestResourceStream(name)
                : File.OpenRead(selected[index]))
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
