
static class BundledTools
{
    public static string Tools;
    public static void ValidateSelectedTools(EncoderSettings settings) =>
        Validate(settings.ChdmanPath, ".exe");

    static void Validate(string path, string extension)
    {
        if (String.IsNullOrWhiteSpace(path)) return;
        string full = Path.GetFullPath(path);
        if (!File.Exists(full) || !Path.GetExtension(full).Equals(extension, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Invalid tool path: " + path);
        FileSystemPaths.EnsureNoLinks(full);
    }

    // Stages chdman in an isolated, integrity-checked cache.
    public static string Stage(EncoderSettings settings = null)
    {
        if (settings != null) ValidateSelectedTools(settings);
        var asm = Assembly.GetExecutingAssembly();
        string id = FileHash(asm.Location)[..16];

        string[] selected = settings == null
            ? new[] { "", "" }
            : new[] { settings.ChdmanPath, "" };
        // Only chdman can be overridden; include its digest in the isolated cache key.
        byte[] signature = String.IsNullOrWhiteSpace(selected[0])
            ? Array.Empty<byte>() : Convert.FromHexString(FileHash(selected[0]));
        id += "-" + Convert.ToHexString(SHA256.HashData(signature))[..12];

        string root = Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE");
        if (String.IsNullOrEmpty(root))
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CHD Optimizer", "tools");

        Tools = Path.Combine(root, id);
        Directory.CreateDirectory(Tools);
        FileSystemPaths.EnsureNoLinks(Tools);
        string[] names = { "chdman.exe", "chd-v4.exe" };
        for (int index = 0; index < names.Length; index++)
        {
            string name = names[index];
            string file = Path.Combine(Tools, name);
            FileSystemPaths.EnsureNoLinks(file);
            using var resource = String.IsNullOrWhiteSpace(selected[index])
                ? asm.GetManifestResourceStream(name)
                : File.OpenRead(selected[index]);
            if (resource == null) throw new IOException("Missing component: " + name);
            using var memory = new MemoryStream();
            resource.CopyTo(memory);
            byte[] data = memory.ToArray();
            string expected = Convert.ToHexString(SHA256.HashData(data));
            if (!File.Exists(file) || FileHash(file) != expected)
                File.WriteAllBytes(file, data);
        }

        return Tools;
    }

    static string FileHash(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
    }
}
