using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;

sealed class ArchiveReader
{
    readonly SevenZipArchive fallback;
    readonly Action<string> report;
    readonly HashSet<string> useFallback = new(StringComparer.OrdinalIgnoreCase);

    public ArchiveReader(ToolRunner tools, Action<string> report)
    {
        fallback = new SevenZipArchive(tools);
        this.report = report;
    }

    public static IEnumerable<string> GetVolumes(string path) => SevenZipArchive.GetVolumes(path);

    static bool Unsupported(Exception error) => error is NotSupportedException
        || error.GetType().Name is "InvalidFormatException" or "IncompleteArchiveException";

    void SelectFallback(string archive, string reason)
    {
        useFallback.Add(archive);
        report("7-Zip de reserva: " + reason);
    }

    static List<ArchiveEntry> Validate(IArchive archive, string destination)
    {
        var entries = new List<ArchiveEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            if (!string.IsNullOrEmpty(entry.LinkTarget)
                || entry is SharpCompress.Common.Zip.ZipEntry zip && ((zip.Attrib.GetValueOrDefault() >> 16) & 0xF000) == 0xA000)
                throw new IOException("Arquivo compactado contém links.");
            if (entry.IsEncrypted)
                throw new IOException("Compactado protegido por senha.");
            string target = FileSystemPaths.ResolveArchivePath(destination, entry.Key);
            if (!seen.Add(target))
                throw new IOException("Caminhos duplicados no compactado.");
            if (!entry.IsDirectory)
                entries.Add(new ArchiveEntry { Path = target, Size = entry.Size });
        }
        if (entries.Count == 0)
            throw new IOException("Compactado vazio.");
        return entries;
    }

    public async Task<List<ArchiveEntry>> ListAsync(string path, string destination)
    {
        if (GetVolumes(path).Skip(1).Any())
            SelectFallback(path, "arquivo em múltiplos volumes");
        if (!useFallback.Contains(path))
        {
            try
            {
                using var archive = ArchiveFactory.Open(path);
                return Validate(archive, destination);
            }
            catch (Exception ex) when (Unsupported(ex)) { SelectFallback(path, ex.Message); }
        }
        return await fallback.ListAsync(path, destination).ConfigureAwait(false);
    }

    public async Task<List<DiscInput>> PreviewAsync(string path, string destination, List<ArchiveEntry> entries)
    {
        if (useFallback.Contains(path))
            return await fallback.PreviewAsync(path, destination, entries).ConfigureAwait(false);
        try
        {
            var cues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (entries.Any(e => MediaFiles.Extension(e.Path) == ".cue"))
            {
                using var archive = ArchiveFactory.Open(path);
                Validate(archive, destination);
                foreach (var entry in archive.Entries.Where(e => MediaFiles.Extension(e.Key) == ".cue"))
                {
                    if (entry.Size > 1048576)
                        throw new IOException("Descritor CUE muito grande.");
                    using var stream = entry.OpenEntryStream();
                    using var reader = new StreamReader(stream);
                    char[] buffer = new char[1048577];
                    int count = await reader.ReadBlockAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (count > 1048576)
                        throw new IOException("Descritor CUE muito grande.");
                    cues[FileSystemPaths.ResolveArchivePath(destination, entry.Key)] = new string(buffer, 0, count);
                }
            }
            return MediaFiles.SelectDiscInputs(entries.Select(e => e.Path), destination, cues);
        }
        catch (Exception ex) when (Unsupported(ex))
        {
            SelectFallback(path, ex.Message);
            await fallback.ListAsync(path, destination).ConfigureAwait(false);
            return await fallback.PreviewAsync(path, destination, entries).ConfigureAwait(false);
        }
    }

    // Valida caminhos antes da escrita e só usa a reserva para incompatibilidades conhecidas.
    public async Task ExtractAsync(string path, string destination)
    {
        if (!useFallback.Contains(path))
        {
            try
            {
                using var archive = ArchiveFactory.Open(path);
                var listed = Validate(archive, destination);
                long total = listed.Sum(e => e.Size), done = 0;
                int lastPercent = -1;
                report("Descompactando com SharpCompress…");
                using var reader = archive.IsSolid || archive.Type == ArchiveType.SevenZip
                    ? archive.ExtractAllEntries()
                    : SharpCompress.Readers.ReaderFactory.Open(File.OpenRead(path),
                        new SharpCompress.Readers.ReaderOptions { LeaveStreamOpen = false });
                byte[] buffer = new byte[131072];
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.IsDirectory)
                        continue;
                    if (!string.IsNullOrEmpty(reader.Entry.LinkTarget))
                        throw new IOException("Link não permitido.");
                    string target = FileSystemPaths.ResolveArchivePath(destination, reader.Entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    FileSystemPaths.EnsureNoLinks(target);
                    using var input = reader.OpenEntryStream();
                    await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    long written = 0;
                    int count;
                    while ((count = await input.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                    {
                        written += count;
                        if (written > reader.Entry.Size)
                            throw new IOException("Tamanho extraído difere da listagem.");
                        await output.WriteAsync(buffer.AsMemory(0, count)).ConfigureAwait(false);
                        done += count;
                        int percent = (int)(100.0 * done / Math.Max(1, total));
                        if (percent != lastPercent)
                        {
                            report("Etapa: " + percent + "%");
                            lastPercent = percent;
                        }
                    }
                    if (written != reader.Entry.Size)
                        throw new IOException("Extração incompleta.");
                }
                return;
            }
            catch (Exception ex) when (Unsupported(ex))
            {
                SelectFallback(path, ex.Message);
                FileSystemPaths.DeleteWorkDirectory(destination, Path.GetDirectoryName(destination));
                Directory.CreateDirectory(destination);
            }
        }
        await fallback.ListAsync(path, destination).ConfigureAwait(false);
        await fallback.ExtractAsync(path, destination).ConfigureAwait(false);
    }
}
