using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

sealed class ArchiveReader
{
    static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    readonly ToolRunner toolRunner;
    public ArchiveReader(ToolRunner toolRunner)
    {
        this.toolRunner = toolRunner;
    }

    public async Task<List<ArchiveEntry>> ListAsync(string archive, string destination)
    {
        string listing = await toolRunner.RequireSuccessAsync("7z.exe", false, "l", "-slt", "-ba",
            "-sccUTF-8", "-p", "--", archive).ConfigureAwait(false);
        var entries = new List<ArchiveEntry>();
        var seen = new HashSet<string>(Paths);
        foreach (string block in Regex.Split(listing, @"\r?\n\s*\r?\n"))
        {
            var p = Regex.Match(block, @"(?m)^Path = (.+)\r?$");
            if (!p.Success)
            {
                continue;
            }

            if (Regex.IsMatch(block, @"(?im)^(Symbolic Link|Hard Link) = .+|^Attributes = .*\bl[rwx-]{9}"))
            {
                throw new IOException("Arquivo compactado contém links.");
            }

            string target = FileSystemPaths.ResolveArchivePath(destination, p.Groups[1].Value.TrimEnd('\r'));
            if (!seen.Add(target))
            {
                throw new IOException("Caminhos duplicados no compactado.");
            }

            if (Regex.IsMatch(block, @"(?m)^Folder = \+|^Attributes = D"))
            {
                continue;
            }

            var size = Regex.Match(block, @"(?m)^Size = (\d+)");
            if (!size.Success)
            {
                throw new IOException("Tamanho ausente na listagem do compactado.");
            }

            entries.Add(new ArchiveEntry
            {
                Path = target,
                Size = Int64.Parse(size.Groups[1].Value)
            });
        }

        if (entries.Count == 0)
        {
            throw new IOException("Compactado vazio ou listagem não reconhecida.");
        }

        return entries;
    }

    public async Task<List<DiscInput>> PreviewAsync(string archive, string destination,
        List<ArchiveEntry> entries)
    {
        var cues = new Dictionary<string, string>(Paths);
        foreach (var e in entries.Where(e => MediaFiles.Extension(e.Path) == ".cue"))
        {
            if (e.Size > 1048576)
            {
                throw new IOException("Descritor CUE muito grande.");
            }

            string relative = e.Path.Substring(destination.TrimEnd('\\').Length + 1);
            cues[e.Path] = await toolRunner.RequireSuccessAsync("7z.exe", false, "x", "-so", "-p",
                "-sccUTF-8", "-spd", "--", archive, relative).ConfigureAwait(false);
        }

        return MediaFiles.SelectDiscInputs(entries.Select(e => e.Path), destination, cues);
    }

    public static IEnumerable<string> GetVolumes(string path)
    {
        string name = Path.GetFileName(path), pattern = null;
        var m = Regex.Match(name, @"(?i)^(.*)\.part0*1\.rar$");
        if (m.Success)
        {
            pattern = "^" + Regex.Escape(m.Groups[1].Value) + @"\.part\d+\.rar$";
        }
        else if ((m = Regex.Match(name, @"(?i)^(.*\.(?:7z|zip))\.001$")).Success)
        {
            pattern = "^" + Regex.Escape(m.Groups[1].Value) + @"\.\d{3}$";
        }
        else if (MediaFiles.Extension(path) == ".rar")
        {
            pattern = "^" + Regex.Escape(Path.GetFileNameWithoutExtension(path)) + @"\.(rar|r\d{2})$";
        }

        return pattern == null ? new[]{path} : Directory.GetFiles(Path.GetDirectoryName(path)).Where(p
            => Regex.IsMatch(Path.GetFileName(p), pattern, RegexOptions.IgnoreCase));
    }
}
