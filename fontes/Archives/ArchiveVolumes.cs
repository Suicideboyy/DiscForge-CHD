using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

static class ArchiveVolumes
{
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
            => Regex.IsMatch(Path.GetFileName(p), pattern, RegexOptions.IgnoreCase)).OrderBy(VolumeNumber);
    }
    // Keep split volumes in numeric order, with the original RAR header first.
    static int VolumeNumber(string path)
    {
        var match = Regex.Match(Path.GetFileName(path), @"(?i)(?:\.part|\.r|\.)(\d+)(?:\.rar)?$");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }
}