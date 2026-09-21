using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

static class MediaFiles
{
    static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    public static string Extension(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant();
    }

    public static bool IsArchive(string p)
    {
        return Regex.IsMatch(p, @"(?i)\.(zip|rar|7z|tar|gz|bz2|xz|tgz|tbz2|txz|7z\.001|zip\.001)$")
            && !Regex.IsMatch(p, @"(?i)\.part0*(?:[2-9]|[1-9]\d+)\.rar$");
    }

    public static bool IsDiscImage(string p)
    {
        return new[]{".chd", ".cue", ".iso", ".bin", ".img"}.Contains(Extension(p));
    }

    public static string OutputStem(string p)
    {
        return IsArchive(p) ? Regex.Replace(Path.GetFileName(p),
            ("(?i)(\\.part0*1\\.rar|\\.(7z|zip)\\.001|\\.tar\\.(gz|bz2|xz)|\\.(zip|rar|7z"
            + "|tar|gz|bz2|xz|tgz|tbz2|txz))$"), "") : Path.GetFileNameWithoutExtension(p);
    }

    public static string NaturalSortKey(string p)
    {
        return Regex.Replace(p, @"\d+", m => m.Value.PadLeft(16, '0'));
    }

    public static string TrackGroup(string p)
    {
        var m = Regex.Match(p, @"(?i)^(.*)\(Track\s+\d+\)\.bin$");
        return m.Success ? m.Groups[1].Value : null;
    }

    public static List<string> ReadCueReferences(string text, string cue, string root)
    {
        var refs = new List<string>();
        foreach (Match m in Regex.Matches(text, @"(?im)^\s*FILE\s+(?:""([^""]+)""|(\S+))\s+\S+"))
        {
            string relative = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (Path.IsPathRooted(relative) || relative.Contains(':'))
            {
                throw new IOException("Referência CUE absoluta não suportada.");
            }

            string p = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cue), relative));
            if (!FileSystemPaths.IsInside(p, root))
            {
                throw new IOException("Referência CUE fora da entrada.");
            }

            FileSystemPaths.EnsureNoLinks(p);
            if (!refs.Contains(p, Paths))
            {
                refs.Add(p);
            }
        }

        if (refs.Count == 0)
        {
            throw new IOException("CUE sem arquivos FILE.");
        }

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
                {
                    throw new IOException("Faixa referenciada não encontrada: " + Path.GetFileName(p));
                }

                companions.Add(p);
            }

            result.Add(new DiscInput
            {
                Path = cue,
                CueText = text
            });
        }

        foreach (string p in list.Where(p => Extension(p) != ".cue" && !companions.Contains(p)))
        {
            string group = TrackGroup(p);
            if (group == null || tracks.Add(group))
            {
                result.Add(new DiscInput
                {
                    Path = p
                });
            }
        }

        return result.OrderBy(m => NaturalSortKey(m.Path), Paths).ToList();
    }

    public static long ReadInfoNumber(string info, string key)
    {
        var m = Regex.Match(info, Regex.Escape(key) + @":\s*([0-9,. ]+)", RegexOptions.IgnoreCase);
        return m.Success ? Int64.Parse(Regex.Replace(m.Groups[1].Value, @"\D", "")) : 0;
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
