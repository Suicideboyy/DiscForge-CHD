
static class MediaFiles
{
    static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    public static string Extension(string path) =>
        Path.GetExtension(path).ToLowerInvariant();

    public static bool IsArchive(string p) =>
        Regex.IsMatch(p, @"(?i)\.(zip|rar|7z|tar|gz|bz2|xz|tgz|tbz2|txz|7z\.001|zip\.001)$")
            && !Regex.IsMatch(p, @"(?i)\.part0*(?:[2-9]|[1-9]\d+)\.rar$");

    public static bool IsDiscImage(string p) =>
        new[]{".chd", ".cue", ".iso", ".bin", ".img"}.Contains(Extension(p));

    public static string OutputStem(string p) =>
        IsArchive(p) ? Regex.Replace(Path.GetFileName(p),
            ("(?i)(\\.part0*1\\.rar|\\.(7z|zip)\\.001|\\.tar\\.(gz|bz2|xz)|\\.(zip|rar|7z"
            + "|tar|gz|bz2|xz|tgz|tbz2|txz))$"), "") : Path.GetFileNameWithoutExtension(p);

    public static string NaturalSortKey(string p) =>
        Regex.Replace(p, @"\d+", m => m.Value.PadLeft(16, '0'));

    public static string TrackGroup(string p)
    {
        var m = Regex.Match(p, @"(?i)^(.*)\(Track\s+\d+\)\.bin$");
        return m.Success ? m.Groups[1].Value : null;
    }

    // Rebuild only track start indices. Without the original CUE, pregaps and additional
    // indices cannot be recovered, so this is for playback rather than archival use.
    public static List<string> ReconstructTrackCues(IEnumerable<string> files, string root)
    {
        var created = new List<string>();
        var all = files.ToList();
        var referenced = all.Where(p => Extension(p) == ".cue")
            .SelectMany(cue => ReadCueReferences(File.ReadAllText(cue), cue, root))
            .ToHashSet(Paths);

        var groups = all.Select(p => new
        {
            Path = p,
            Match = Regex.Match(Path.GetFileName(p), @"(?i)^(.*)\(Track\s+(\d+)\)\.bin$")
        }).Where(x => x.Match.Success).GroupBy(x =>
            Path.Combine(Path.GetDirectoryName(x.Path), x.Match.Groups[1].Value), Paths);

        foreach (var group in groups)
        {
            if (group.Any(x => referenced.Contains(x.Path)))
                continue;

            var tracks = group.Select(x => new
            {
                x.Path,
                Number = Int32.Parse(x.Match.Groups[2].Value)
            }).OrderBy(x => x.Number).ToList();
            if (tracks[0].Number != 1 || tracks.Select((x, i) => x.Number == i + 1).Any(ok => !ok))
                throw new IOException("BIN tracks are not consecutively numbered: " + group.Key);

            var cue = new StringBuilder();
            foreach (var track in tracks)
            {
                var info = new FileInfo(track.Path);
                if (info.Length == 0 || info.Length % 2352 != 0)
                    throw new IOException("BIN track does not use 2352-byte RAW sectors: " + info.Name);

                int mode = ReadRawMode(track.Path, 300);

                string kind = mode > 0 ? "MODE" + mode + "/2352" : "AUDIO";
                if (track.Number == 1 && kind == "AUDIO")
                    throw new IOException("First track has no RAW data sector; original CUE required.");

                cue.Append("FILE \"").Append(info.Name).Append("\" BINARY\r\n")
                    .Append("  TRACK ").Append(track.Number.ToString("D2")).Append(' ').Append(kind)
                    .Append("\r\n    INDEX 01 00:00:00\r\n");
            }

            string path = group.Key.TrimEnd(' ', '.') + " (reconstructed).cue";
            if (File.Exists(path))
                throw new IOException("Reconstructed CUE already exists: " + path);

            File.WriteAllText(path, cue.ToString(), new UTF8Encoding(false));
            created.Add(path);
        }

        return created;
    }

    public static List<string> ReadCueReferences(string text, string cue, string root)
    {
        var refs = new List<string>();
        foreach (Match m in Regex.Matches(text, @"(?im)^\s*FILE\s+(?:""([^""]+)""|(\S+))\s+\S+"))
        {
            string relative = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (Path.IsPathRooted(relative) || relative.Contains(':'))
                throw new IOException("Absolute CUE references are not supported.");

            string p = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cue), relative));
            if (!FileSystemPaths.IsInside(p, root))
                throw new IOException("CUE reference is outside the input directory.");

            FileSystemPaths.EnsureNoLinks(p);
            if (!refs.Contains(p, Paths))
                refs.Add(p);
        }

        if (refs.Count == 0)

            throw new IOException("CUE contains no FILE entries.");

        return refs;
    }

    public static List<DiscInput> SelectDiscInputs(IEnumerable<string> files, string root,
        Dictionary<string, string> cueTexts)
    {
        var list = files.Where(IsDiscImage).OrderBy(NaturalSortKey, Paths).ToList();
        var companions = new HashSet<string>(Paths);
        var result = new List<DiscInput>();
        var tracks = new HashSet<string>(Paths);
        foreach (string cue in list.Where(p => Extension(p) == ".cue"))
        {
            string text = cueTexts == null ? File.ReadAllText(cue) : cueTexts[cue];
            foreach (string p in ReadCueReferences(text, cue, root))
            {
                if (!list.Contains(p, Paths))
                    throw new IOException("Referenced track not found: " + Path.GetFileName(p));

                companions.Add(p);
            }

            result.Add(new DiscInput { Path = cue, CueText = text });
        }

        foreach (string p in list.Where(p => Extension(p) != ".cue" && !companions.Contains(p)))
        {
            string group = TrackGroup(p);
            if (group == null || tracks.Add(group))
                result.Add(new DiscInput { Path = p });
        }

        return result.OrderBy(m => NaturalSortKey(m.Path), Paths).ToList();
    }

    public static long ReadInfoNumber(string info, string key)
    {
        var m = Regex.Match(info, Regex.Escape(key) + @":\s*([0-9,. ]+)", RegexOptions.IgnoreCase);
        return m.Success ? Int64.Parse(Regex.Replace(m.Groups[1].Value, @"\D", "")) : 0;
    }

    // Share RAW sector validation across classification, reconstruction and encoding.
    public static int ReadRawMode(string path, int sectors = 1)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length == 0 || stream.Length % 2352 != 0) return 0;
        Span<byte> header = stackalloc byte[16];
        for (long index = 0; index < Math.Min(stream.Length / 2352, sectors); index++)
        {
            stream.Position = index * 2352;
            stream.ReadExactly(header);
            if (header[0] == 0 && header[11] == 0 && header[15] is 1 or 2
                && header.Slice(1, 10).IndexOfAnyExcept((byte)255) < 0)
                return header[15];
        }
        return 0;
    }

    public static string NormalizeSerial(string text)
    {
        var set = new HashSet<string>();
        foreach (Match m in Regex.Matches((text ?? "").ToUpperInvariant(),
            @"(?<![A-Z0-9])([A-Z]{4})[-_ ]?([0-9]{3})[. _-]?([0-9]{2})(?![A-Z0-9])"))
        {
            set.Add(m.Groups[1].Value + "-" + m.Groups[2].Value + m.Groups[3].Value);
        }

        return set.Count == 1 ? set.First() : "";
    }
}
