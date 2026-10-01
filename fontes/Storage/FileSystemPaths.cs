
static class FileSystemPaths
{
    static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    public static void EnsureNoLinks(string path)
    {
        for (var d = new FileInfo(Path.GetFullPath(path)); d != null; d = d.Directory == null ? null
            : new FileInfo(d.Directory.FullName))
        {
            if ((File.Exists(d.FullName) || Directory.Exists(d.FullName))
                && (File.GetAttributes(d.FullName) & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("Link/junction not allowed: " + d.FullName);
            }
        }
    }

    public static bool IsInside(string path, string root)
    {
        return Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd('\\') + "\\",
            StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveArchivePath(string root, string relative)
    {
        if (String.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(':'))
        {
            throw new IOException("Absolute or invalid archive path: " + relative);
        }

        string p = Path.GetFullPath(Path.Combine(root, relative.Replace('/', '\\')));
        if (!IsInside(p, root))
        {
            throw new IOException("Caminho fora da pasta permitida: " + relative);
        }

        EnsureNoLinks(p);
        return p;
    }

    public static IEnumerable<string> EnumerateFiles(string root)
    {
        EnsureNoLinks(root);
        foreach (string f in Directory.GetFiles(root))
        {
            EnsureNoLinks(f);
            yield return f;
        }

        foreach (string d in Directory.GetDirectories(root))
        {
            EnsureNoLinks(d);
            foreach (string f in EnumerateFiles(d))
            {
                yield return f;
            }
        }
    }

    public static void DeleteWorkDirectory(string directory, string parent)
    {
        if (!IsInside(directory, parent))
            throw new IOException("Temporary folder is outside the expected location.");

        // Native tools can release their final directory handle shortly after exit.
        // Retry only temporary cleanup; every pass checks the boundary and links again.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                DeleteWorkDirectoryOnce(directory, parent);
                return;
            }
            catch (Exception ex) when (attempt < 3
                && (ex is IOException || ex is UnauthorizedAccessException))
            {
                Thread.Sleep(125 * (attempt + 1));
            }
        }
    }

    static void DeleteWorkDirectoryOnce(string directory, string parent)
    {
        if (!IsInside(directory, parent))
        {
            throw new IOException("Temporary folder is outside the expected location.");
        }

        EnsureNoLinks(directory);
        if (!Directory.Exists(directory)) return;
        File.SetAttributes(directory, File.GetAttributes(directory) & ~FileAttributes.ReadOnly);
        foreach (string f in Directory.GetFiles(directory))
        {
            EnsureNoLinks(f);
            File.SetAttributes(f, File.GetAttributes(f) & ~FileAttributes.ReadOnly);
            File.Delete(f);
        }

        foreach (string d in Directory.GetDirectories(directory))
        {
            DeleteWorkDirectoryOnce(d, parent);
        }

        Directory.Delete(directory, false);
    }
}
