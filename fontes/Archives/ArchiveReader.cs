using SharpCompress.Archives;
using SharpCompress.Common;

sealed class ArchiveReader
{
    readonly Action<string> report;

    public ArchiveReader(Action<string> report) => this.report = report;

    public static IEnumerable<string> GetVolumes(string path) => ArchiveVolumes.GetVolumes(path);

    static IArchive OpenArchive(string path)
    {
        var volumes = GetVolumes(path).Select(p => new FileInfo(p)).ToArray();
        return volumes.Length > 1 ? ArchiveFactory.Open(volumes) : ArchiveFactory.Open(path);
    }

    // Validate paths before creating files; archives may not contain links or duplicate names.
    static List<ArchiveEntry> Validate(IArchive archive, string destination)
    {
        var entries = new List<ArchiveEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            Engine.ThrowIfCancelled();
            if (!string.IsNullOrEmpty(entry.LinkTarget)
                || entry is SharpCompress.Common.Zip.ZipEntry zip
                && ((zip.Attrib.GetValueOrDefault() >> 16) & 0xF000) == 0xA000)
                throw new IOException("Archive contains links.");
            if (entry.IsEncrypted)
                throw new IOException("Archive requires a password.");
            string target = FileSystemPaths.ResolveArchivePath(destination, entry.Key);
            if (!seen.Add(target))
                throw new IOException("Duplicate archive paths.");
            if (!entry.IsDirectory)
                entries.Add(new ArchiveEntry { Path = target, Size = entry.Size });
        }
        if (entries.Count == 0)
            throw new IOException("Empty archive.");
        return entries;
    }

    public Task<List<ArchiveEntry>> ListAsync(string path, string destination)
    {
        Engine.ThrowIfCancelled();
        using var archive = OpenArchive(path);
        return Task.FromResult(Validate(archive, destination));
    }

    // Solid and 7z formats require a sequential reader; ordinary RARs use independent entries.
    public async Task ExtractAsync(string path, string destination)
    {
        Engine.ThrowIfCancelled();
        using var archive = OpenArchive(path);
        var listed = Validate(archive, destination);
        long total = listed.Sum(e => e.Size), done = 0;
        int lastPercent = -1;
        byte[] buffer = new byte[131072];
        report("Extracting with SharpCompress...");
        if (archive.IsSolid || archive.Type == ArchiveType.SevenZip)
        {
            using var reader = archive.ExtractAllEntries();
            while (reader.MoveToNextEntry())
            {
                Engine.ThrowIfCancelled();
                if (reader.Entry.IsDirectory) continue;
                using var input = reader.OpenEntryStream();
                await CopyEntryAsync(reader.Entry, input).ConfigureAwait(false);
            }
        }
        else
        {
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                Engine.ThrowIfCancelled();
                using var input = entry.OpenEntryStream();
                await CopyEntryAsync(entry, input).ConfigureAwait(false);
            }
        }

        // Read to EOF so SharpCompress performs integrity checks before encoding begins.
        async Task CopyEntryAsync(IEntry entry, Stream input)
        {
            if (!string.IsNullOrEmpty(entry.LinkTarget))
                throw new IOException("Link not allowed.");
            string target = FileSystemPaths.ResolveArchivePath(destination, entry.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            FileSystemPaths.EnsureNoLinks(target);
            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
            long written = 0;
            int count;
            // SharpCompress RAR async reads can overrun entry boundaries; use checked sync reads.
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                Engine.ThrowIfCancelled();
                written += count;
                if (written > entry.Size)
                    throw new IOException("Extracted size differs from archive listing.");
                await output.WriteAsync(buffer.AsMemory(0, count), Engine.Token).ConfigureAwait(false);
                done += count;
                int percent = (int)(100.0 * done / Math.Max(1, total));
                if (percent != lastPercent)
                {
                    report("Etapa: " + percent + "%");
                    lastPercent = percent;
                }
            }
            if (written != entry.Size)
                throw new IOException("Incomplete extraction.");
        }
    }
}
